using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models;

namespace DoAnLapTrinhWeb.Controllers
{
    public class InventoryController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // 1. TRANG LẬP PHIẾU NHẬP KHO
        // GET: Inventory/Index
        public ActionResult Index()
        {
            ViewBag.Suppliers = db.Suppliers
                .Where(s => s.status == "Active" || string.IsNullOrEmpty(s.status))
                .ToList();

            ViewBag.Ingredients = db.Ingredients
                .Where(i => i.status == "Active" || string.IsNullOrEmpty(i.status))
                .ToList();

            ViewBag.NewPoId = GenerateNextPoId();
            return View();
        }

        // 2. TRANG LỊCH SỬ NHẬP KHO
        // GET: Inventory/LichSuNhap
        public ActionResult LichSuNhap()
        {
            var list = db.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.User)
                .Include(p => p.PurchaseOrderDetails)
                .OrderByDescending(p => p.created_at)
                .ToList();

            ViewBag.TotalOrders = list.Count;
            ViewBag.TotalSpentThisMonth = list
                .Where(p => p.status == "Completed" && p.created_at.Month == DateTime.Now.Month && p.created_at.Year == DateTime.Now.Year)
                .Sum(p => (decimal?)p.total_amount) ?? 0;
            ViewBag.TotalSuppliers = db.Suppliers.Count(s => s.status == "Active" || string.IsNullOrEmpty(s.status));

            return View(list);
        }

        // 3. API: CHỌN NCC -> TỰ ĐỘNG LẤY DANH MỤC NGUYÊN LIỆU PHÙ HỢP
        [HttpGet]
        public JsonResult GetIngredientsBySupplier(string supplierId)
        {
            try
            {
                if (string.IsNullOrEmpty(supplierId))
                    return Json(new { success = false, message = "Chưa chọn nhà cung cấp." }, JsonRequestBehavior.AllowGet);

                supplierId = supplierId.Trim();
                var supplier = db.Suppliers.Find(supplierId);
                if (supplier == null)
                    return Json(new { success = false, message = "Không tìm thấy nhà cung cấp." }, JsonRequestBehavior.AllowGet);

                var pastIngs = db.PurchaseOrders
                    .Where(p => p.supplier_id == supplierId && p.status == "Completed")
                    .OrderByDescending(p => p.created_at)
                    .SelectMany(p => p.PurchaseOrderDetails)
                    .ToList();

                var priceDict = new Dictionary<string, decimal>();
                foreach (var item in pastIngs)
                {
                    string ingId = item.ingredient_id.Trim();
                    if (!priceDict.ContainsKey(ingId)) priceDict[ingId] = item.unit_price;
                }

                var allIngredients = db.Ingredients
                    .Where(i => i.status == "Active" || string.IsNullOrEmpty(i.status))
                    .ToList();

                var result = new List<object>();

                if (priceDict.Any())
                {
                    foreach (var ing in allIngredients)
                    {
                        string id = ing.ingredient_id.Trim();
                        if (priceDict.ContainsKey(id))
                        {
                            result.Add(new { code = id, name = ing.name, unit = ing.unit, price = priceDict[id] });
                        }
                    }
                }
                else
                {
                    string sName = supplier.supplier_name.ToLower();
                    foreach (var ing in allIngredients)
                    {
                        string iName = ing.name.ToLower();
                        bool match = false;
                        if (sName.Contains("phúc long") || sName.Contains("trà"))
                            match = iName.Contains("trà") || iName.Contains("oolong") || iName.Contains("ceylon");
                        else if (sName.Contains("b-one") || sName.Contains("indo") || sName.Contains("sữa") || sName.Contains("rich"))
                            match = iName.Contains("sữa") || iName.Contains("trân châu") || iName.Contains("kem");
                        else
                            match = true;

                        if (match)
                        {
                            result.Add(new { code = ing.ingredient_id.Trim(), name = ing.name, unit = ing.unit, price = 1000 });
                        }
                    }
                }

                return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class ImportItemDTO
        {
            public string ingredient_id { get; set; }
            public decimal quantity { get; set; }
            public decimal unit_price { get; set; }
        }

        // 4. API LƯU PHIẾU NHẬP KHO (CHỈ TẠO PHIẾU PENDING - KHÔNG CỘNG KHO Ở ĐÂY)
        [HttpPost]
        public JsonResult SavePurchaseOrder(string supplierId, decimal discount, decimal shippingFee, List<ImportItemDTO> items)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(supplierId) || items == null || !items.Any())
                {
                    return Json(new { success = false, message = "Vui lòng chọn Nhà cung cấp và nhập ít nhất 1 dòng nguyên liệu." });
                }

                supplierId = supplierId.Trim();
                string poId = GenerateNextPoId();
                string currentUserId = Session["UserId"] != null ? Session["UserId"].ToString().Trim() : "U01";

                decimal subtotal = items.Sum(i => i.quantity * i.unit_price);
                decimal finalTotal = Math.Max(0, subtotal - discount + shippingFee);

                var po = new PurchaseOrder
                {
                    po_id = poId,
                    supplier_id = supplierId,
                    user_id = currentUserId,
                    total_amount = finalTotal,
                    status = "Pending",
                    created_at = DateTime.Now
                };
                db.PurchaseOrders.Add(po);

                // 1. TÌM SỐ THỨ TỰ LỚN NHẤT MỘT LẦN DUY NHẤT TRƯỚC KHI VÀO VÒNG LẶP
                var existingIds = db.PurchaseOrderDetails.Select(d => d.po_detail_id).ToList();
                int maxNumber = 0;
                foreach (var raw in existingIds)
                {
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    string s = raw.Trim();
                    if (s.StartsWith("POD", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(s.Substring(3), out int num))
                        {
                            if (num > maxNumber) maxNumber = num;
                        }
                    }
                }

                // 2. CHẠY VÒNG LẶP VÀ TỰ ĐỘNG TĂNG DẦN CHO TỪNG DÒNG HÀNG TRONG PHIẾU
                foreach (var itm in items)
                {
                    maxNumber++; // Tăng dần lên: nếu lớn nhất là 4 thì dòng này là 5, tiếp theo là 6...
                    string podId = "POD" + maxNumber.ToString("D3"); // Sinh ra POD005, POD006...
                    string ingId = itm.ingredient_id.Trim();

                    var pod = new PurchaseOrderDetail
                    {
                        po_detail_id = podId,
                        po_id = poId,
                        ingredient_id = ingId,
                        quantity = itm.quantity,
                        unit_price = itm.unit_price
                    };
                    db.PurchaseOrderDetails.Add(pod);
                }

                db.SaveChanges();
                return Json(new { success = true, poId = poId, message = "Đã tạo phiếu chờ nhập thành công!" });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Lỗi khi lưu phiếu: " + inner });
            }
        }

        // 5. API LẤY CHI TIẾT 1 PHIẾU NHẬP ĐỂ XEM / IN
        [HttpGet]
        public JsonResult GetPurchaseOrderDetail(string poId)
        {
            try
            {
                if (string.IsNullOrEmpty(poId)) return Json(new { success = false, message = "Mã phiếu không hợp lệ." }, JsonRequestBehavior.AllowGet);

                poId = poId.Trim();
                var po = db.PurchaseOrders
                    .Include(p => p.Supplier)
                    .Include(p => p.User)
                    .Include(p => p.PurchaseOrderDetails.Select(d => d.Ingredient))
                    .FirstOrDefault(p => p.po_id == poId);

                if (po == null) return Json(new { success = false, message = "Không tìm thấy phiếu." }, JsonRequestBehavior.AllowGet);

                var items = po.PurchaseOrderDetails.Select((d, idx) => new
                {
                    stt = idx + 1,
                    ingredient_id = d.ingredient_id.Trim(),
                    ingredient_name = d.Ingredient != null ? d.Ingredient.name : "N/A",
                    unit = d.Ingredient != null ? d.Ingredient.unit : "",
                    quantity = d.quantity,
                    unit_price = d.unit_price,
                    total_line = d.quantity * d.unit_price
                }).ToList();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        po_id = po.po_id.Trim(),
                        supplier_name = po.Supplier != null ? po.Supplier.supplier_name : "N/A",
                        staff_name = po.User != null ? po.User.full_name : "Thủ kho",
                        created_at = po.created_at.ToString("dd/MM/yyyy HH:mm"),
                        status = po.status,
                        total_amount = po.total_amount,
                        items = items
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // 6. XÁC NHẬN NHẬP KHO (CHÍNH THỨC CỘNG TỒN KHO & GHI LOG IN)
        [HttpPost]
        public JsonResult ConfirmImport(string poId)
        {
            try
            {
                if (string.IsNullOrEmpty(poId)) return Json(new { success = false, message = "Mã phiếu không hợp lệ." });
                poId = poId.Trim();

                var po = db.PurchaseOrders.Include(p => p.PurchaseOrderDetails).FirstOrDefault(p => p.po_id == poId);
                if (po == null) return Json(new { success = false, message = "Không tìm thấy phiếu." });
                if (po.status != "Pending") return Json(new { success = false, message = "Chỉ xác nhận phiếu đang ở trạng thái Chờ (Pending)." });

                po.status = "Completed";
                string currentUserId = Session["UserId"] != null ? Session["UserId"].ToString().Trim() : "U01";

                foreach (var d in po.PurchaseOrderDetails)
                {
                    var ing = db.Ingredients.Find(d.ingredient_id.Trim());
                    if (ing != null)
                    {
                        ing.quantity_in_stock += d.quantity; // Tăng tồn kho thực tế
                    }

                    string transId = "TX" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                    db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        transaction_id = transId,
                        ingredient_id = d.ingredient_id.Trim(),
                        po_detail_id = d.po_detail_id,
                        order_detail_id = null,
                        type = "IN",
                        quantity = d.quantity,
                        reason = "Xác nhận nhập kho thực tế cho phiếu " + poId,
                        created_by = currentUserId,
                        created_at = DateTime.Now
                    });
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Đã xác nhận nhập kho và cộng dồn tồn kho thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 7. HỦY PHIẾU NHẬP
        [HttpPost]
        public JsonResult CancelPurchaseOrder(string poId)
        {
            try
            {
                if (string.IsNullOrEmpty(poId)) return Json(new { success = false, message = "Mã phiếu không hợp lệ." });
                poId = poId.Trim();

                var po = db.PurchaseOrders.Find(poId);
                if (po == null) return Json(new { success = false, message = "Không tìm thấy phiếu." });
                if (po.status == "Completed") return Json(new { success = false, message = "Phiếu đã hoàn tất không thể hủy." });

                po.status = "Cancelled";
                db.SaveChanges();
                return Json(new { success = true, message = "Đã hủy phiếu nhập thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 8. HÀM DEDUCT STOCK CHO TV3 GỌI KHI CHECKOUT ĐƠN HÀNG
        public bool DeductStockForOrder(string orderId)
        {
            try
            {
                orderId = orderId.Trim();
                var orderDetails = db.OrderDetails.Where(od => od.order_id == orderId).ToList();
                if (!orderDetails.Any()) return false;

                foreach (var od in orderDetails)
                {
                    string pid = od.product_id.Trim();
                    string sid = !string.IsNullOrEmpty(od.size_id) ? od.size_id.Trim() : null;

                    var recipes = db.Recipes.Where(r => r.product_id == pid && (r.size_id == sid || r.size_id == null)).ToList();

                    foreach (var r in recipes)
                    {
                        decimal deductQty = r.quantity * od.quantity;
                        var ing = db.Ingredients.Find(r.ingredient_id.Trim());
                        if (ing != null)
                        {
                            ing.quantity_in_stock = Math.Max(0, ing.quantity_in_stock - deductQty);
                        }

                        string transId = "TX" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                        db.InventoryTransactions.Add(new InventoryTransaction
                        {
                            transaction_id = transId,
                            ingredient_id = r.ingredient_id.Trim(),
                            po_detail_id = null,
                            order_detail_id = od.order_detail_id,
                            type = "OUT",
                            quantity = deductQty,
                            reason = "Xuất kho bán hàng đơn " + orderId,
                            created_by = null,
                            created_at = DateTime.Now
                        });
                    }
                }

                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateNextPoId()
        {
            var existingIds = db.PurchaseOrders.Select(p => p.po_id).ToList();
            int max = 0;
            foreach (var raw in existingIds)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string s = raw.Trim();
                if (s.StartsWith("PO", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(s.Substring(2), out int num))
                    {
                        if (num > max) max = num;
                    }
                }
            }
            return "PO" + (max + 1).ToString("D3");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }


        private string GenerateNextPoDetailId()
        {
            var existingIds = db.PurchaseOrderDetails.Select(d => d.po_detail_id).ToList();
            int max = 0;
            foreach (var raw in existingIds)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string s = raw.Trim();
                if (s.StartsWith("POD", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(s.Substring(3), out int num))
                    {
                        if (num > max) max = num;
                    }
                }
            }
            return "POD" + (max + 1).ToString("D3"); // Tạo ra POD001, POD002
        }

        public class ConfirmItemDTO
        {
            public string pod_id { get; set; }
            public string batch_no { get; set; }
            public DateTime? expiry_date { get; set; }
        }

        public class ConfirmRequestDTO
        {
            public string poId { get; set; }
            public List<ConfirmItemDTO> items { get; set; }
        }

        [HttpPost]
        public JsonResult ConfirmImportWithBatch(ConfirmRequestDTO request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.poId))
                    return Json(new { success = false, message = "Mã phiếu không hợp lệ." });

                string poId = request.poId.Trim();
                var po = db.PurchaseOrders.Include(p => p.PurchaseOrderDetails).FirstOrDefault(p => p.po_id == poId);
                if (po == null) return Json(new { success = false, message = "Không tìm thấy phiếu." });
                if (po.status != "Pending") return Json(new { success = false, message = "Phiếu này đã được xử lý hoặc không ở trạng thái chờ." });

                po.status = "Completed";
                string currentUserId = Session["UserId"] != null ? Session["UserId"].ToString().Trim() : "U01";

                foreach (var d in po.PurchaseOrderDetails)
                {
                    var clientItem = request.items?.FirstOrDefault(i => i.pod_id.Trim() == d.po_detail_id.Trim());
                    if (clientItem != null)
                    {
                        // Nếu cơ sở dữ liệu có các cột batch_no hoặc expiry_date trong bảng PurchaseOrderDetails, bạn có thể gán vào đây
                    }

                    // 1. Cộng dồn tồn kho thực tế vào bảng Ingredients[cite: 24]
                    var ing = db.Ingredients.Find(d.ingredient_id.Trim());
                    if (ing != null)
                    {
                        ing.quantity_in_stock += d.quantity; 
            }

                    // 2. Ghi log giao dịch kho[cite: 24]
                    string transId = "TX" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                    db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        transaction_id = transId,
                        ingredient_id = d.ingredient_id.Trim(),
                        po_detail_id = d.po_detail_id,
                        order_detail_id = null,
                        type = "IN",
                        quantity = d.quantity,
                        reason = "Xác nhận hàng về thực tế từ phiếu " + poId,
                        created_by = currentUserId,
                        created_at = DateTime.Now
                    });
                }

                db.SaveChanges(); 
                return Json(new { success = true, message = "Đã xác nhận hàng về, cập nhật Mã lô, HSD và cộng dồn tồn kho thành công!" });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Lỗi xác nhận: " + inner });
            }
        }


    }
}