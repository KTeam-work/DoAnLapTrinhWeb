using System;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models;

namespace DoAnLapTrinhWeb.Controllers
{
    public class ProductController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();
        // TỰ SINH MÃ SẢN PHẨM: PRD01, PRD02, PRD03...
        private string GenerateProductId()
        {
            var productIds = db.Products
                .Select(p => p.product_id)
                .ToList();

            int maxNumber = 0;

            foreach (var id in productIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                string trimmedId = id.Trim();

                // Chỉ lấy mã có định dạng PRD + số
                if (trimmedId.StartsWith("PRD", StringComparison.OrdinalIgnoreCase))
                {
                    string numberPart = trimmedId.Substring(3);

                    if (int.TryParse(numberPart, out int number))
                    {
                        if (number > maxNumber)
                            maxNumber = number;
                    }
                }
            }

            return "PRD" + (maxNumber + 1).ToString("D2");
        }

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
                products = products.Where(p => (p.product_id != null && p.product_id.ToLower().Contains(kw))
                                            || (p.name != null && p.name.ToLower().Contains(kw)));
            }

            // 2. LỌC THEO LOẠI SẢN PHẨM
            if (!string.IsNullOrWhiteSpace(category))
            {
                products = products.Where(p => p.Category != null && p.Category.category_name == category);
            }

            // 3. LỌC THEO TRẠNG THÁI
            if (!string.IsNullOrWhiteSpace(status))
            {
                string st = status.Trim().ToLower();

                // Nhóm ngừng bán
                if (st == "ngừng bán" || st == "ngung ban" || st == "unavailable" || st == "inactive")
                {
                    products = products.Where(p => p.status != null &&
                        (p.status.Trim().ToLower() == "unavailable" ||
                         p.status.Trim().ToLower() == "inactive" ||
                         p.status.Trim().ToLower() == "ngừng bán"));
                }
                // Nhóm đang bán
                else if (st == "đang bán" || st == "dang ban" || st == "available" || st == "active")
                {
                    products = products.Where(p => p.status != null &&
                        (p.status.Trim().ToLower() == "available" ||
                         p.status.Trim().ToLower() == "active" ||
                         p.status.Trim().ToLower() == "đang bán"));
                }
                else
                {
                    products = products.Where(p => p.status != null && p.status.Trim().ToLower() == st);
                }
            }

            // GỬI LẠI THÔNG TIN FILTER RA VIEW
            ViewBag.Keyword = keyword;
            ViewBag.Category = category;
            ViewBag.Status = status;

            // DANH SÁCH DANH MỤC CHO DROPDOWN
            ViewBag.Categories = db.Categories.ToList();

            // THỐNG KÊ TỔNG QUAN
            var allProducts = db.Products.ToList();

            ViewBag.TotalProducts = allProducts.Count;

            ViewBag.ActiveProducts = allProducts.Count(p =>
                string.Equals(
                    (p.status ?? "").Trim(),
                    "Available",
                    StringComparison.OrdinalIgnoreCase
                )
            );

            ViewBag.InactiveProducts = allProducts.Count(p =>
                string.Equals(
                    (p.status ?? "").Trim(),
                    "Unavailable",
                    StringComparison.OrdinalIgnoreCase
                )
            );


            // TỰ SINH MÃ SẢN PHẨM MỚI
            ViewBag.NextProductId = GenerateProductId();

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
                // Kiểm tra trạng thái sản phẩm
                string productStatus = string.IsNullOrWhiteSpace(status)
                    ? "Available"
                    : status.Trim();

                if (productStatus != "Available" && productStatus != "Unavailable")
                {
                    return Json(new
                    {
                        success = false,
                        message = "Trạng thái sản phẩm không hợp lệ."
                    });
                }

                // Tạo sản phẩm mới
                string newProductId = GenerateProductId();

                Product newProd = new Product
                {
                    product_id = newProductId,
                    name = name.Trim(),
                    category_id = catId,
                    description = description ?? "",
                    status = productStatus
                };

                db.Products.Add(newProd);

                // 3. Chuẩn bị tự sinh mã size_id cho bảng ProductSizes
                var lastSize = db.ProductSizes.OrderByDescending(s => s.size_id).FirstOrDefault();
                int nextSizeNum = 1;
                if (lastSize != null && !string.IsNullOrEmpty(lastSize.size_id))
                {
                    string numStr = Regex.Match(lastSize.size_id, @"\d+").Value;
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
                        status = "Active"
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
                if (!string.IsNullOrWhiteSpace(toppings))
                {
                    var toppingNames = toppings.Split(
                        new char[] { ',' },
                        StringSplitOptions.RemoveEmptyEntries
                    );

                    foreach (var tName in toppingNames)
                    {
                        string toppingName = tName.Trim();

                        var topObj = db.Toppings.FirstOrDefault(t =>
                            t.name.Trim().ToLower() == toppingName.ToLower()
                            && t.status == "Active"
                        );

                        if (topObj == null)
                        {
                            return Json(new
                            {
                                success = false,
                                message = "Không tìm thấy topping đang hoạt động: " + toppingName
                            });
                        }

                        db.ProductToppings.Add(new ProductTopping
                        {
                            id = "PT" + Guid.NewGuid().ToString("N")
                                .Substring(0, 8).ToUpper(),

                            product_id = newProd.product_id,
                            topping_id = topObj.topping_id,
                            is_allowed = true
                        });
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

                prod.name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : prod.name;

                var cat = db.Categories.FirstOrDefault(c => c.category_name.Trim().ToLower() == category_name.Trim().ToLower());
                if (cat != null)
                {
                    prod.category_id = cat.category_id;
                }

                prod.description = description ?? "";

                // Chỉ chấp nhận hai trạng thái hợp lệ của SQL Server
                if (!string.IsNullOrWhiteSpace(status))
                {
                    status = status.Trim();

                    if (status != "Available" && status != "Unavailable")
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Trạng thái sản phẩm không hợp lệ."
                        });
                    }

                    prod.status = status;
                }

                db.Entry(prod).State = EntityState.Modified;

                var currentSizes = db.ProductSizes.Where(s => s.product_id.Trim() == trimmedId).ToList();

                // Size S
                var sizeS = currentSizes.FirstOrDefault(s =>
    !string.IsNullOrWhiteSpace(s.size_name) &&
    (s.size_name.Trim() == "S" || s.size_name.Trim() == "Size S"));
                if (priceS.HasValue)
                {
                    if (sizeS != null) { sizeS.price_add = priceS.Value; db.Entry(sizeS).State = EntityState.Modified; }
                    else { db.ProductSizes.Add(new ProductSize { size_id = "PS" + Guid.NewGuid().ToString("N").Substring(0, 8), product_id = trimmedId, size_name = "S", price_add = priceS.Value, status = "Active" }); }
                }

                // Size M
                var sizeM = currentSizes.FirstOrDefault(s =>
    !string.IsNullOrWhiteSpace(s.size_name) &&
    (s.size_name.Trim() == "M" || s.size_name.Trim() == "Size M"));
                if (priceM.HasValue)
                {
                    if (sizeM != null) { sizeM.price_add = priceM.Value; db.Entry(sizeM).State = EntityState.Modified; }
                    else { db.ProductSizes.Add(new ProductSize { size_id = "PS" + Guid.NewGuid().ToString("N").Substring(0, 8), product_id = trimmedId, size_name = "M", price_add = priceM.Value, status = "Active" }); }
                }

                // Size L
                var sizeL = currentSizes.FirstOrDefault(s =>
    !string.IsNullOrWhiteSpace(s.size_name) &&
    (s.size_name.Trim() == "L" || s.size_name.Trim() == "Size L"));
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
                Exception inner = ex;

                while (inner.InnerException != null)
                {
                    inner = inner.InnerException;
                }

                return Json(new
                {
                    success = false,
                    message = "Lỗi khi cập nhật: " + inner.Message
                });
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
                if (string.IsNullOrWhiteSpace(id))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Mã sản phẩm không hợp lệ."
                    });
                }

                string trimmedId = id.Trim();

                var prod = db.Products.FirstOrDefault(
                    p => p.product_id.Trim() == trimmedId
                );

                if (prod == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy sản phẩm cần xóa."
                    });
                }

                // Không xóa sản phẩm đã phát sinh đơn hàng
                bool hasOrder = db.OrderDetails.Any(
                    od => od.product_id.Trim() == trimmedId
                );

                if (hasOrder)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không thể xóa sản phẩm vì sản phẩm đã phát sinh đơn hàng. Em có thể chuyển trạng thái sang Ngừng bán."
                    });
                }

                // Không xóa sản phẩm đã có đánh giá
                bool hasReview = db.ProductReviews.Any(
                    r => r.product_id.Trim() == trimmedId
                );

                if (hasReview)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không thể xóa sản phẩm vì sản phẩm đã có đánh giá. Em có thể chuyển trạng thái sang Ngừng bán."
                    });
                }

                // Xóa các size thuộc sản phẩm
                var sizes = db.ProductSizes.Where(
                    s => s.product_id.Trim() == trimmedId
                ).ToList();

                db.ProductSizes.RemoveRange(sizes);

                // Xóa các topping liên kết với sản phẩm
                var toppings = db.ProductToppings.Where(
                    t => t.product_id.Trim() == trimmedId
                ).ToList();

                db.ProductToppings.RemoveRange(toppings);

                // Xóa sản phẩm
                db.Products.Remove(prod);

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Xóa sản phẩm thành công!"
                });
            }
            catch (Exception ex)
            {
                Exception inner = ex;

                while (inner.InnerException != null)
                {
                    inner = inner.InnerException;
                }

                return Json(new
                {
                    success = false,
                    message = "Lỗi khi xóa: " + inner.Message
                });
            }
        }

        [HttpPost]
        public ActionResult SubmitReview(string product_id, int rating, string comment)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để đánh giá sản phẩm." });
                }

                string customerId = Session["UserId"].ToString().Trim();

                if (rating < 1 || rating > 5)
                {
                    return Json(new { success = false, message = "Số sao đánh giá phải từ 1 đến 5 sao!" });
                }

                string pId = product_id.Trim();

                bool hasPurchased = db.OrderDetails
                    .Any(od => od.product_id.Trim() == pId && od.Order.customer_id.Trim() == customerId);

                if (!hasPurchased)
                {
                    return Json(new { success = false, message = "Bạn chỉ có thể đánh giá sau khi đã mua và sử dụng sản phẩm này!" });
                }

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
                     .Where(p => p.status == "Available")
                     .ToList();
        }
    }
}