using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models;

namespace DoAnLapTrinhWeb.Controllers
{
    public class ProductController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // =========================================================
        // INDEX: Danh sách sản phẩm, Tìm kiếm & Lọc từ SQL Server
        // =========================================================
        public ActionResult Index(string keyword = "", string category = "", string status = "")
        {
            var products = db.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .Include(p => p.ProductToppings)
                .AsQueryable();

            // 1. TÌM KIẾM THEO MÃ HOẶC TÊN SẢN PHẨM
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                products = products.Where(p => p.product_id.ToLower().Contains(kw) || p.name.ToLower().Contains(kw));
            }

            // 2. LỌC THEO LOẠI SẢN PHẨM
            if (!string.IsNullOrWhiteSpace(category))
            {
                products = products.Where(p => p.Category != null && p.Category.category_name == category);
            }

            // 3. LỌC THEO TRẠNG THÁI
            if (!string.IsNullOrWhiteSpace(status))
            {
                products = products.Where(p => p.status == status);
            }

            // GỬI LẠI THÔNG TIN FILTER RA VIEW
            ViewBag.Keyword = keyword;
            ViewBag.Category = category;
            ViewBag.Status = status;

            // DANH SÁCH DANH MỤC CHO DROPDOWN
            ViewBag.Categories = db.Categories.ToList();

            // THỐNG KÊ TỔNG QUAN
            ViewBag.TotalProducts = db.Products.Count();
            ViewBag.ActiveProducts = db.Products.Count(p => p.status == "Active" || p.status == "Đang bán");
            ViewBag.InactiveProducts = db.Products.Count(p => p.status == "Inactive" || p.status == "Ngừng bán");

            // TỰ SINH MÃ SẢN PHẨM MỚI CHO MODAL THÊM
            var lastProduct = db.Products.OrderByDescending(p => p.product_id).FirstOrDefault();
            string nextId = "SP0000001";
            if (lastProduct != null && !string.IsNullOrEmpty(lastProduct.product_id))
            {
                int num = int.Parse(lastProduct.product_id.Trim().Replace("SP", "")) + 1;
                nextId = "SP" + num.ToString("D7");
            }
            ViewBag.NextProductId = nextId;

            return View(products.ToList());
        }

        // =========================================================
        // CREATE (POST): Thêm sản phẩm mới kèm Giá/Size vào CSDL
        // =========================================================
        [HttpPost]
        public ActionResult Create(string product_id, string name, string category_name, string description, string status, decimal? priceS, decimal? priceM, decimal? priceL, string toppings)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "Tên sản phẩm không được để trống." });
                }

                // 1. Lấy category_id
                var cat = db.Categories.FirstOrDefault(c => c.category_name.Trim().ToLower() == category_name.Trim().ToLower());
                string catId = cat != null ? cat.category_id : db.Categories.Select(c => c.category_id).FirstOrDefault();

                // 2. Tạo sản phẩm mới
                Product newProd = new Product
                {
                    product_id = !string.IsNullOrEmpty(product_id) ? product_id.Trim() : "SP0000001",
                    name = name.Trim(),
                    category_id = catId,
                    description = description ?? "",
                    status = string.IsNullOrEmpty(status) ? "Đang bán" : status
                };

                db.Products.Add(newProd);

                // 3. Chuẩn bị tự sinh mã size_id cho bảng ProductSizes
                var lastSize = db.ProductSizes.OrderByDescending(s => s.size_id).FirstOrDefault();
                int nextSizeNum = 1;
                if (lastSize != null && !string.IsNullOrEmpty(lastSize.size_id))
                {
                    // Lấy phần số trong mã size_id (Ví dụ: PS0000001 -> 1)
                    string numStr = System.Text.RegularExpressions.Regex.Match(lastSize.size_id, @"\d+").Value;
                    if (!string.IsNullOrEmpty(numStr))
                    {
                        int.TryParse(numStr, out nextSizeNum);
                        nextSizeNum++;
                    }
                }

                // Thêm Size S
                if (priceS.HasValue)
                {
                    db.ProductSizes.Add(new ProductSize
                    {
                        size_id = "PS" + (nextSizeNum++).ToString("D7"),
                        product_id = newProd.product_id,
                        size_name = "S",
                        price_add = priceS.Value,
                        status = "Active" // Hoặc "Đang bán" tùy DB của em quy định
                    });
                }

                // Thêm Size M
                if (priceM.HasValue)
                {
                    db.ProductSizes.Add(new ProductSize
                    {
                        size_id = "PS" + (nextSizeNum++).ToString("D7"),
                        product_id = newProd.product_id,
                        size_name = "M",
                        price_add = priceM.Value,
                        status = "Active"
                    });
                }

                // Thêm Size L
                if (priceL.HasValue)
                {
                    db.ProductSizes.Add(new ProductSize
                    {
                        size_id = "PS" + (nextSizeNum++).ToString("D7"),
                        product_id = newProd.product_id,
                        size_name = "L",
                        price_add = priceL.Value,
                        status = "Active"
                    });
                }

                // 4. Thêm Topping
                if (!string.IsNullOrEmpty(toppings))
                {
                    var toppingNames = toppings.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var tName in toppingNames)
                    {
                        var topObj = db.Toppings.FirstOrDefault(t => t.name.Trim().ToLower() == tName.Trim().ToLower());
                        if (topObj != null)
                        {
                            db.ProductToppings.Add(new ProductTopping
                            {
                                product_id = newProd.product_id,
                                topping_id = topObj.topping_id
                            });
                        }
                    }
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Thêm sản phẩm thành công!" });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                string errorDetails = "";
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errorDetails += $"- Cột [{validationError.PropertyName}]: {validationError.ErrorMessage}\n";
                    }
                }
                return Json(new { success = false, message = "CSDL báo lỗi Validation:\n" + errorDetails });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        // =========================================================
        // TRANG DETAILS SẢN PHẨM: Chi tiết kèm Size, Topping
        // =========================================================
        public ActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index");
            }

            string trimmedId = id.Trim();
            var product = db.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .Include(p => p.ProductToppings.Select(pt => pt.Topping))
                .Include(p => p.ProductReviews.Select(r => r.Customer))
                .FirstOrDefault(p => p.product_id.Trim() == trimmedId);

            if (product == null)
            {
                return HttpNotFound();
            }
            ViewBag.Categories = db.Categories.ToList();

            return View(product);
        }

        // =========================================================
        // 3. EDIT (POST): Cập nhật thông tin sản phẩm trong CSDL
        // =========================================================
        [HttpPost]
        public ActionResult Edit(string product_id, string name, string category_name, string description, string status, decimal? priceS, decimal? priceM, decimal? priceL)
        {
            try
            {
                if (string.IsNullOrEmpty(product_id))
                {
                    return Json(new { success = false, message = "Mã sản phẩm không hợp lệ." });
                }

                string trimmedId = product_id.Trim();
                var prod = db.Products.FirstOrDefault(p => p.product_id.Trim() == trimmedId);

                if (prod == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm cần sửa." });
                }

                // Cập nhật thông tin cơ bản
                prod.name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : prod.name;

                var cat = db.Categories.FirstOrDefault(c => c.category_name.Trim().ToLower() == category_name.Trim().ToLower());
                if (cat != null)
                {
                    prod.category_id = cat.category_id;
                }

                prod.description = description ?? "";
                prod.status = status ?? prod.status;

                db.Entry(prod).State = EntityState.Modified;

                // Cập nhật giá theo Size trong ProductSizes
                var currentSizes = db.ProductSizes.Where(s => s.product_id.Trim() == trimmedId).ToList();

                // Xử lý Size S
                var sizeS = currentSizes.FirstOrDefault(s => s.size_name == "S");
                if (priceS.HasValue)
                {
                    if (sizeS != null) { sizeS.price_add = priceS.Value; db.Entry(sizeS).State = EntityState.Modified; }
                    else { db.ProductSizes.Add(new ProductSize { size_id = "PS" + Guid.NewGuid().ToString("N").Substring(0, 8), product_id = trimmedId, size_name = "S", price_add = priceS.Value, status = "Active" }); }
                }

                // Xử lý Size M
                var sizeM = currentSizes.FirstOrDefault(s => s.size_name == "M");
                if (priceM.HasValue)
                {
                    if (sizeM != null) { sizeM.price_add = priceM.Value; db.Entry(sizeM).State = EntityState.Modified; }
                    else { db.ProductSizes.Add(new ProductSize { size_id = "PS" + Guid.NewGuid().ToString("N").Substring(0, 8), product_id = trimmedId, size_name = "M", price_add = priceM.Value, status = "Active" }); }
                }

                // Xử lý Size L
                var sizeL = currentSizes.FirstOrDefault(s => s.size_name == "L");
                if (priceL.HasValue)
                {
                    if (sizeL != null) { sizeL.price_add = priceL.Value; db.Entry(sizeL).State = EntityState.Modified; }
                    else { db.ProductSizes.Add(new ProductSize { size_id = "PS" + Guid.NewGuid().ToString("N").Substring(0, 8), product_id = trimmedId, size_name = "L", price_add = priceL.Value, status = "Active" }); }
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Cập nhật sản phẩm thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật: " + ex.Message });
            }
        }

        // =========================================================
        // 4. DELETE (POST): Xóa sản phẩm khỏi CSDL
        // =========================================================
        [HttpPost]
        public ActionResult Delete(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return Json(new { success = false, message = "Mã sản phẩm không hợp lệ." });
                }

                string trimmedId = id.Trim();
                var prod = db.Products.FirstOrDefault(p => p.product_id.Trim() == trimmedId);

                if (prod != null)
                {
                    // Xóa dữ liệu liên quan ở bảng ProductSizes và ProductToppings
                    var sizes = db.ProductSizes.Where(s => s.product_id.Trim() == trimmedId);
                    db.ProductSizes.RemoveRange(sizes);

                    var toppings = db.ProductToppings.Where(t => t.product_id.Trim() == trimmedId);
                    db.ProductToppings.RemoveRange(toppings);

                    // Xóa sản phẩm
                    db.Products.Remove(prod);
                    db.SaveChanges();

                    return Json(new { success = true, message = "Xóa sản phẩm thành công!" });
                }

                return Json(new { success = false, message = "Không tìm thấy sản phẩm cần xóa." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult SubmitReview(string product_id, int rating, string comment)
        {
            try
            {
                // 1. Check đăng nhập (Session key "UserId" thống nhất với TV1)
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để đánh giá sản phẩm." });
                }

                string customerId = Session["UserId"].ToString().Trim();

                // 2. Validate rating từ 1 đến 5 sao
                if (rating < 1 || rating > 5)
                {
                    return Json(new { success = false, message = "Số sao đánh giá phải từ 1 đến 5 sao!" });
                }

                string pId = product_id.Trim();

                // 3. LUỒNG QUAN TRỌNG: Check OrderDetails xem khách đã mua sản phẩm này chưa
                bool hasPurchased = db.OrderDetails
                    .Any(od => od.product_id.Trim() == pId && od.Order.customer_id.Trim() == customerId);

                if (!hasPurchased)
                {
                    return Json(new { success = false, message = "Bạn chỉ có thể đánh giá sau khi đã mua và sử dụng sản phẩm này!" });
                }

                // 4. Lưu đánh giá vào CSDL
                var review = new ProductReview
                {
                    review_id = "RV" + Guid.NewGuid().ToString("N").Substring(0, 7).ToUpper(),
                    product_id = pId,
                    customer_id = customerId,
                    rating = rating,
                    comment = comment != null ? comment.Trim() : "",
                    created_at = DateTime.Now
                };

                db.ProductReviews.Add(review);
                db.SaveChanges();

                return Json(new { success = true, message = "Gửi đánh giá thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
    public static class ProductService
    {
        private static DoAnLapTrinhWeb.Models.QUANLYBANTRASUAEntities db = new DoAnLapTrinhWeb.Models.QUANLYBANTRASUAEntities();

        public static DoAnLapTrinhWeb.Models.Product GetById(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return null;
            string id = productId.Trim();
            return db.Products
                     .Include("ProductSizes")
                     .Include("ProductToppings")
                     .FirstOrDefault(p => p.product_id.Trim() == id);
        }

        public static System.Collections.Generic.List<DoAnLapTrinhWeb.Models.Product> GetAvailable()
        {
            return db.Products
                     .Where(p => p.status == "Active" || p.status == "Đang bán")
                     .ToList();
        }
    }
}