
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models;
using System.IO;
using System.Collections.Generic;

namespace DoAnLapTrinhWeb.Controllers
{
    public class ProductController : Controller
    {
        private QUANLYBANTRASUAEntities db =
            new QUANLYBANTRASUAEntities();

        // =========================================================
        // TỰ SINH MÃ SẢN PHẨM: PRD01, PRD02, PRD03...
        // =========================================================
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

                if (trimmedId.StartsWith(
                    "PRD", StringComparison.OrdinalIgnoreCase))
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
        // TỰ SINH MÃ SIZE THEO MÃ SẢN PHẨM
        // PRD01 -> SZ_S01, SZ_M01, SZ_L01
        // PRD16 -> SZ_S16, SZ_M16, SZ_L16
        // =========================================================
        private string GenerateSizeId(
            string productId,
            string sizeName)
        {
            if (string.IsNullOrWhiteSpace(productId))
                throw new ArgumentException(
                    "Mã sản phẩm không hợp lệ.");

            string trimmedProductId = productId.Trim();

            if (!trimmedProductId.StartsWith(
                "PRD", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Mã sản phẩm phải bắt đầu bằng PRD.");
            }

            string productNumber =
                trimmedProductId.Substring(3);

            if (!int.TryParse(productNumber, out _))
            {
                throw new ArgumentException(
                    "Phần số của mã sản phẩm không hợp lệ.");
            }

            string size = (sizeName ?? "")
                .Trim()
                .ToUpperInvariant();

            if (size != "S" && size != "M" && size != "L")
            {
                throw new ArgumentException(
                    "Size không hợp lệ.");
            }

            return "SZ_" + size + productNumber;
        }

        // =========================================================
        // KIỂM TRA VÀ CHUẨN HÓA ĐƯỜNG DẪN ẢNH TRONG ASSETS
        // =========================================================
        private bool TryGetAssetImagePath(
            string imageUrl,
            out string savedImagePath,
            out string errorMessage)
        {
            savedImagePath = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(imageUrl))
                return true;

            string imageName = imageUrl.Trim()
                .Replace('\\', '/');

            if (imageName.StartsWith(
                "~/Assets/", StringComparison.OrdinalIgnoreCase))
            {
                imageName = imageName.Substring(
                    "~/Assets/".Length);
            }
            else if (imageName.StartsWith(
                "/Assets/", StringComparison.OrdinalIgnoreCase))
            {
                imageName = imageName.Substring(
                    "/Assets/".Length);
            }
            else
            {
                errorMessage = "Đường dẫn ảnh không hợp lệ.";
                return false;
            }

            if (Path.GetFileName(imageName) != imageName)
            {
                errorMessage = "Tên ảnh không hợp lệ.";
                return false;
            }

            string extension =
                Path.GetExtension(imageName).ToLowerInvariant();

            if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }
                .Contains(extension))
            {
                errorMessage =
                    "Định dạng ảnh không được hỗ trợ.";
                return false;
            }

            string assetsPath = Server.MapPath("~/Assets");

            string physicalPath =
                Path.Combine(assetsPath, imageName);

            if (!System.IO.File.Exists(physicalPath))
            {
                errorMessage =
                    "Không tìm thấy ảnh trong thư mục Assets.";
                return false;
            }

            savedImagePath = "~/Assets/" + imageName;

            return true;
        }

        // =========================================================
        // INDEX: DANH SÁCH SẢN PHẨM, TÌM KIẾM VÀ LỌC
        // =========================================================
        public ActionResult Index(
            string keyword = "",
            string category = "",
            string status = "")
        {
            var products = db.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .Include(p => p.ProductToppings)
                .AsQueryable();

            // TÌM KIẾM THEO MÃ HOẶC TÊN
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();

                products = products.Where(p =>
                    (p.product_id != null &&
                     p.product_id.ToLower().Contains(kw))
                    ||
                    (p.name != null &&
                     p.name.ToLower().Contains(kw)));
            }

            // LỌC THEO DANH MỤC
            if (!string.IsNullOrWhiteSpace(category))
            {
                products = products.Where(p =>
                    p.Category != null &&
                    p.Category.category_name == category);
            }

            // LỌC THEO TRẠNG THÁI
            if (!string.IsNullOrWhiteSpace(status))
            {
                string st = status.Trim().ToLower();

                if (st == "ngừng bán" ||
                    st == "ngung ban" ||
                    st == "unavailable" ||
                    st == "inactive")
                {
                    products = products.Where(p =>
                        p.status != null &&
                        (
                            p.status.Trim().ToLower() == "unavailable" ||
                            p.status.Trim().ToLower() == "inactive" ||
                            p.status.Trim().ToLower() == "ngừng bán"
                        ));
                }
                else if (st == "đang bán" ||
                         st == "dang ban" ||
                         st == "available" ||
                         st == "active")
                {
                    products = products.Where(p =>
                        p.status != null &&
                        (
                            p.status.Trim().ToLower() == "available" ||
                            p.status.Trim().ToLower() == "active" ||
                            p.status.Trim().ToLower() == "đang bán"
                        ));
                }
                else
                {
                    products = products.Where(p =>
                        p.status != null &&
                        p.status.Trim().ToLower() == st);
                }
            }

            // GỬI FILTER RA VIEW
            ViewBag.Keyword = keyword;
            ViewBag.Category = category;
            ViewBag.Status = status;

            ViewBag.Categories = db.Categories.ToList();

            // THỐNG KÊ
            var allProducts = db.Products.ToList();

            ViewBag.TotalProducts = allProducts.Count;

            ViewBag.ActiveProducts = allProducts.Count(p =>
                string.Equals(
                    (p.status ?? "").Trim(),
                    "Available",
                    StringComparison.OrdinalIgnoreCase));

            ViewBag.InactiveProducts = allProducts.Count(p =>
                string.Equals(
                    (p.status ?? "").Trim(),
                    "Unavailable",
                    StringComparison.OrdinalIgnoreCase));

            // MÃ SẢN PHẨM TIẾP THEO
            ViewBag.NextProductId = GenerateProductId();

            // DANH SÁCH ẢNH TRONG ASSETS
            string assetsPath = Server.MapPath("~/Assets");

            if (Directory.Exists(assetsPath))
            {
                ViewBag.ProductImages = Directory
                    .GetFiles(assetsPath)
                    .Where(file =>
                        new[] { ".jpg", ".jpeg", ".png", ".webp" }
                        .Contains(
                            Path.GetExtension(file).ToLowerInvariant()))
                    .Select(file => Path.GetFileName(file))
                    .OrderBy(file => file)
                    .ToList();
            }
            else
            {
                ViewBag.ProductImages =
                    new List<string>();
            }

            return View(products.ToList());
        }

        // =========================================================
        // CREATE: THÊM SẢN PHẨM MỚI
        // =========================================================
        [HttpPost]
        public ActionResult Create(
            string name,
            string category_name,
            string description,
            string status,
            decimal? priceS,
            decimal? priceM,
            decimal? priceL,
            string toppings,
            string image_url)
        {
            try
            {
                // KIỂM TRA TÊN
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Tên sản phẩm không được để trống."
                    });
                }

                // KIỂM TRA GIÁ
                if ((priceS.HasValue && priceS.Value < 0) ||
                    (priceM.HasValue && priceM.Value < 0) ||
                    (priceL.HasValue && priceL.Value < 0))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Giá sản phẩm không được âm."
                    });
                }

                // KIỂM TRA ẢNH
                string savedImagePath;

                if (!TryGetAssetImagePath(
                    image_url,
                    out savedImagePath,
                    out string imageError))
                {
                    return Json(new
                    {
                        success = false,
                        message = imageError
                    });
                }

                // LẤY DANH MỤC
                if (string.IsNullOrWhiteSpace(category_name))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng chọn loại sản phẩm."
                    });
                }

                string categoryName = category_name.Trim();

                var cat = db.Categories.FirstOrDefault(c =>
                    c.category_name.Trim().ToLower() ==
                    categoryName.ToLower());

                if (cat == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy loại sản phẩm."
                    });
                }

                // KIỂM TRA TRẠNG THÁI
                string productStatus =
                    string.IsNullOrWhiteSpace(status)
                    ? "Available"
                    : status.Trim();

                if (productStatus != "Available" &&
                    productStatus != "Unavailable")
                {
                    return Json(new
                    {
                        success = false,
                        message = "Trạng thái sản phẩm không hợp lệ."
                    });
                }

                // TẠO MÃ SẢN PHẨM
                string newProductId = GenerateProductId();

                // LẤY GIÁ NHỎ NHẤT LÀM GIÁ CƠ BẢN
                var enteredPrices = new List<decimal>();

                if (priceS.HasValue)
                    enteredPrices.Add(priceS.Value);

                if (priceM.HasValue)
                    enteredPrices.Add(priceM.Value);

                if (priceL.HasValue)
                    enteredPrices.Add(priceL.Value);

                decimal basePrice = enteredPrices.Any()
                    ? enteredPrices.Min()
                    : 0m;

                // TẠO SẢN PHẨM
                Product newProd = new Product
                {
                    product_id = newProductId,
                    name = name.Trim(),
                    category_id = cat.category_id,
                    base_price = basePrice,
                    description = description ?? "",
                    status = productStatus,
                    image_url = savedImagePath
                };

                db.Products.Add(newProd);

                // =================================================
                // THÊM SIZE S
                // =================================================
                if (priceS.HasValue)
                {
                    db.ProductSizes.Add(new ProductSize
                    {
                        size_id = GenerateSizeId(
                            newProductId, "S"),

                        product_id = newProductId,
                        size_name = "S",
                        price_add = priceS.Value,
                        status = "Active"
                    });
                }

                // =================================================
                // THÊM SIZE M
                // =================================================
                if (priceM.HasValue)
                {
                    db.ProductSizes.Add(new ProductSize
                    {
                        size_id = GenerateSizeId(
                            newProductId, "M"),

                        product_id = newProductId,
                        size_name = "M",
                        price_add = priceM.Value,
                        status = "Active"
                    });
                }

                // =================================================
                // THÊM SIZE L
                // =================================================
                if (priceL.HasValue)
                {
                    db.ProductSizes.Add(new ProductSize
                    {
                        size_id = GenerateSizeId(
                            newProductId, "L"),

                        product_id = newProductId,
                        size_name = "L",
                        price_add = priceL.Value,
                        status = "Active"
                    });
                }

                // =================================================
                // THÊM TOPPING
                // =================================================
                if (!string.IsNullOrWhiteSpace(toppings))
                {
                    var toppingNames = toppings.Split(
                        new char[] { ',' },
                        StringSplitOptions.RemoveEmptyEntries);

                    foreach (var tName in toppingNames)
                    {
                        string toppingName = tName.Trim();

                        var topObj = db.Toppings.FirstOrDefault(t =>
                            t.name.Trim().ToLower() ==
                            toppingName.ToLower() &&
                            t.status == "Active");

                        if (topObj == null)
                        {
                            return Json(new
                            {
                                success = false,
                                message =
                                    "Không tìm thấy topping đang hoạt động: "
                                    + toppingName
                            });
                        }

                        db.ProductToppings.Add(new ProductTopping
                        {
                            id = "PT" +
                                Guid.NewGuid().ToString("N")
                                .Substring(0, 8).ToUpper(),

                            product_id = newProductId,
                            topping_id = topObj.topping_id,
                            is_allowed = true
                        });
                    }
                }

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Thêm sản phẩm thành công!"
                });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                string errorDetails = "";

                foreach (var validationErrors in
                    dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in
                        validationErrors.ValidationErrors)
                    {
                        errorDetails +=
                            "- Cột [" +
                            validationError.PropertyName +
                            "]: " +
                            validationError.ErrorMessage +
                            "\n";
                    }
                }

                return Json(new
                {
                    success = false,
                    message =
                        "CSDL báo lỗi Validation:\n" + errorDetails
                });
            }
            catch (Exception ex)
            {
                Exception inner = ex;

                while (inner.InnerException != null)
                    inner = inner.InnerException;

                return Json(new
                {
                    success = false,
                    message = "Lỗi chi tiết: " + inner.Message
                });
            }
        }

        // =========================================================
        // DETAILS: CHI TIẾT SẢN PHẨM
        // =========================================================
        public ActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction("Index");

            string trimmedId = id.Trim();

            var product = db.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .Include(p => p.ProductToppings.Select(
                    pt => pt.Topping))
                .Include(p => p.ProductReviews.Select(
                    r => r.Customer))
                .FirstOrDefault(p =>
                    p.product_id.Trim() == trimmedId);

            if (product == null)
                return HttpNotFound();

            ViewBag.Categories = db.Categories.ToList();

            // DANH SÁCH ẢNH ĐỂ CHỌN KHI CHỈNH SỬA
            string assetsPath = Server.MapPath("~/Assets");

            if (Directory.Exists(assetsPath))
            {
                ViewBag.ProductImages = Directory
                    .GetFiles(assetsPath)
                    .Where(file =>
                        new[] { ".jpg", ".jpeg", ".png", ".webp" }
                        .Contains(
                            Path.GetExtension(file).ToLowerInvariant()))
                    .Select(file => Path.GetFileName(file))
                    .OrderBy(file => file)
                    .ToList();
            }
            else
            {
                ViewBag.ProductImages = new List<string>();
            }

            return View(product);
        }

        // =========================================================
        // EDIT: CẬP NHẬT SẢN PHẨM, SIZE VÀ ẢNH
        // =========================================================
        [HttpPost]
        public ActionResult Edit(
            string product_id,
            string name,
            string category_name,
            string description,
            string status,
            decimal? priceS,
            decimal? priceM,
            decimal? priceL,
            string image_url,
            bool remove_image = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(product_id))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Mã sản phẩm không hợp lệ."
                    });
                }

                string trimmedId = product_id.Trim();

                var prod = db.Products.FirstOrDefault(p =>
                    p.product_id.Trim() == trimmedId);

                if (prod == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy sản phẩm cần sửa."
                    });
                }

                // KIỂM TRA TÊN
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Tên sản phẩm không được để trống."
                    });
                }

                // KIỂM TRA GIÁ
                if ((priceS.HasValue && priceS.Value < 0) ||
                    (priceM.HasValue && priceM.Value < 0) ||
                    (priceL.HasValue && priceL.Value < 0))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Giá sản phẩm không được âm."
                    });
                }

                // =================================================
                // CẬP NHẬT ẢNH
                // =================================================
                if (remove_image)
                {
                    // Người dùng chọn bỏ ảnh
                    prod.image_url = null;
                }
                else if (!string.IsNullOrWhiteSpace(image_url))
                {
                    string savedImagePath;
                    string imageError;

                    if (!TryGetAssetImagePath(
                        image_url,
                        out savedImagePath,
                        out imageError))
                    {
                        return Json(new
                        {
                            success = false,
                            message = imageError
                        });
                    }

                    prod.image_url = savedImagePath;
                }
                // Nếu image_url rỗng và remove_image = false
                // thì giữ nguyên ảnh cũ.

                // =================================================
                // CẬP NHẬT TÊN
                // =================================================
                prod.name = name.Trim();

                // =================================================
                // CẬP NHẬT DANH MỤC
                // =================================================
                if (!string.IsNullOrWhiteSpace(category_name))
                {
                    string categoryName = category_name.Trim();

                    var cat = db.Categories.FirstOrDefault(c =>
                        c.category_name.Trim().ToLower() ==
                        categoryName.ToLower());

                    if (cat == null)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Không tìm thấy loại sản phẩm."
                        });
                    }

                    prod.category_id = cat.category_id;
                }

                // CẬP NHẬT MÔ TẢ
                prod.description = description ?? "";

                // =================================================
                // CẬP NHẬT TRẠNG THÁI
                // =================================================
                if (!string.IsNullOrWhiteSpace(status))
                {
                    status = status.Trim();

                    if (status != "Available" &&
                        status != "Unavailable")
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Trạng thái sản phẩm không hợp lệ."
                        });
                    }

                    prod.status = status;
                }

                // =================================================
                // LẤY DANH SÁCH SIZE HIỆN TẠI
                // =================================================
                var currentSizes = db.ProductSizes
                    .Where(s =>
                        s.product_id.Trim() == trimmedId)
                    .ToList();

                // =================================================
                // CẬP NHẬT SIZE S
                // =================================================
                var sizeS = currentSizes.FirstOrDefault(s =>
                    !string.IsNullOrWhiteSpace(s.size_name) &&
                    (
                        s.size_name.Trim() == "S" ||
                        s.size_name.Trim() == "Size S"
                    ));

                if (priceS.HasValue)
                {
                    if (sizeS != null)
                    {
                        sizeS.price_add = priceS.Value;
                    }
                    else
                    {
                        db.ProductSizes.Add(new ProductSize
                        {
                            size_id = GenerateSizeId(
                                trimmedId, "S"),

                            product_id = trimmedId,
                            size_name = "S",
                            price_add = priceS.Value,
                            status = "Active"
                        });
                    }
                }

                // =================================================
                // CẬP NHẬT SIZE M
                // =================================================
                var sizeM = currentSizes.FirstOrDefault(s =>
                    !string.IsNullOrWhiteSpace(s.size_name) &&
                    (
                        s.size_name.Trim() == "M" ||
                        s.size_name.Trim() == "Size M"
                    ));

                if (priceM.HasValue)
                {
                    if (sizeM != null)
                    {
                        sizeM.price_add = priceM.Value;
                    }
                    else
                    {
                        db.ProductSizes.Add(new ProductSize
                        {
                            size_id = GenerateSizeId(
                                trimmedId, "M"),

                            product_id = trimmedId,
                            size_name = "M",
                            price_add = priceM.Value,
                            status = "Active"
                        });
                    }
                }

                // =================================================
                // CẬP NHẬT SIZE L
                // =================================================
                var sizeL = currentSizes.FirstOrDefault(s =>
                    !string.IsNullOrWhiteSpace(s.size_name) &&
                    (
                        s.size_name.Trim() == "L" ||
                        s.size_name.Trim() == "Size L"
                    ));

                if (priceL.HasValue)
                {
                    if (sizeL != null)
                    {
                        sizeL.price_add = priceL.Value;
                    }
                    else
                    {
                        db.ProductSizes.Add(new ProductSize
                        {
                            size_id = GenerateSizeId(
                                trimmedId, "L"),

                            product_id = trimmedId,
                            size_name = "L",
                            price_add = priceL.Value,
                            status = "Active"
                        });
                    }
                }

                // CẬP NHẬT GIÁ CƠ BẢN THEO GIÁ NHỎ NHẤT
                var updatedPrices = new List<decimal>();

                if (priceS.HasValue)
                    updatedPrices.Add(priceS.Value);
                else if (sizeS != null)
                    updatedPrices.Add(sizeS.price_add);

                if (priceM.HasValue)
                    updatedPrices.Add(priceM.Value);
                else if (sizeM != null)
                    updatedPrices.Add(sizeM.price_add);

                if (priceL.HasValue)
                    updatedPrices.Add(priceL.Value);
                else if (sizeL != null)
                    updatedPrices.Add(sizeL.price_add);

                if (updatedPrices.Any())
                    prod.base_price = updatedPrices.Min();

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Cập nhật sản phẩm thành công!",
                    redirectUrl = Url.Action("Index", "Product")
                });
            }
            catch (Exception ex)
            {
                Exception inner = ex;

                while (inner.InnerException != null)
                    inner = inner.InnerException;

                return Json(new
                {
                    success = false,
                    message = "Lỗi khi cập nhật: " + inner.Message
                });
            }
        }

        // =========================================================
        // DELETE: XÓA SẢN PHẨM
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

                var prod = db.Products.FirstOrDefault(p =>
                    p.product_id.Trim() == trimmedId);

                if (prod == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy sản phẩm cần xóa."
                    });
                }

                // KIỂM TRA ĐƠN HÀNG
                bool hasOrder = db.OrderDetails.Any(od =>
                    od.product_id.Trim() == trimmedId);

                if (hasOrder)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Không thể xóa sản phẩm vì sản phẩm đã phát sinh đơn hàng. Em có thể chuyển trạng thái sang Ngừng bán."
                    });
                }

                // KIỂM TRA ĐÁNH GIÁ
                bool hasReview = db.ProductReviews.Any(r =>
                    r.product_id.Trim() == trimmedId);

                if (hasReview)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Không thể xóa sản phẩm vì sản phẩm đã có đánh giá. Em có thể chuyển trạng thái sang Ngừng bán."
                    });
                }

                // XÓA SIZE
                var sizes = db.ProductSizes
                    .Where(s =>
                        s.product_id.Trim() == trimmedId)
                    .ToList();

                db.ProductSizes.RemoveRange(sizes);

                // XÓA TOPPING LIÊN KẾT
                var toppings = db.ProductToppings
                    .Where(t =>
                        t.product_id.Trim() == trimmedId)
                    .ToList();

                db.ProductToppings.RemoveRange(toppings);

                // XÓA SẢN PHẨM
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
                    inner = inner.InnerException;

                return Json(new
                {
                    success = false,
                    message = "Lỗi khi xóa: " + inner.Message
                });
            }
        }

        // =========================================================
        // SUBMIT REVIEW: ĐÁNH GIÁ SẢN PHẨM
        // =========================================================
        [HttpPost]
        public ActionResult SubmitReview(
            string product_id,
            int rating,
            string comment)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Vui lòng đăng nhập để đánh giá sản phẩm."
                    });
                }

                if (string.IsNullOrWhiteSpace(product_id))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Mã sản phẩm không hợp lệ."
                    });
                }

                string customerId =
                    Session["UserId"].ToString().Trim();

                if (rating < 1 || rating > 5)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Số sao đánh giá phải từ 1 đến 5 sao!"
                    });
                }

                string pId = product_id.Trim();

                bool hasPurchased = db.OrderDetails.Any(od =>
                    od.product_id.Trim() == pId &&
                    od.Order.customer_id.Trim() == customerId);

                if (!hasPurchased)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Bạn chỉ có thể đánh giá sau khi đã mua và sử dụng sản phẩm này!"
                    });
                }

                var review = new ProductReview
                {
                    review_id = "RV" +
                        Guid.NewGuid().ToString("N")
                        .Substring(0, 7).ToUpper(),

                    product_id = pId,
                    customer_id = customerId,
                    rating = rating,
                    comment = comment != null
                        ? comment.Trim()
                        : "",
                    created_at = DateTime.Now
                };

                db.ProductReviews.Add(review);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Gửi đánh giá thành công!"
                });
            }
            catch (Exception ex)
            {
                Exception inner = ex;

                while (inner.InnerException != null)
                    inner = inner.InnerException;

                return Json(new
                {
                    success = false,
                    message = "Lỗi hệ thống: " + inner.Message
                });
            }
        }

        // =========================================================
        // DISPOSE DATABASE
        // =========================================================
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    // =============================================================
    // PRODUCT SERVICE
    // =============================================================
    public static class ProductService
    {
        private static DoAnLapTrinhWeb.Models.QUANLYBANTRASUAEntities db =
            new DoAnLapTrinhWeb.Models.QUANLYBANTRASUAEntities();

        public static DoAnLapTrinhWeb.Models.Product GetById(
            string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
                return null;

            string id = productId.Trim();

            return db.Products
                .Include("ProductSizes")
                .Include("ProductToppings")
                .FirstOrDefault(p =>
                    p.product_id.Trim() == id);
        }

        public static List<DoAnLapTrinhWeb.Models.Product>
            GetAvailable()
        {
            return db.Products
                .Where(p => p.status == "Available")
                .ToList();
        }
    }
}
