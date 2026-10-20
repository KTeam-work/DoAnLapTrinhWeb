using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models;

namespace DoAnLapTrinhWeb.Controllers
{
    public class SizeController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // 1. INDEX: Lấy danh sách size từ CSDL
        public ActionResult Index()
        {
            // Lấy danh sách từ bảng ProductSizes (hoặc bảng Size riêng nếu DB của em có bảng chuẩn hóa Size)
            // Dưới đây giả lập theo cấu trúc product size hoặc bảng quản lý size riêng
            var sizes = db.ProductSizes
                .Include(s => s.Product)
                .ToList()
                .Select(s => new
                {
                    Id = s.size_id.Trim(),
                    Name = "Size " + s.size_name,
                    Letter = s.size_name,
                    Description = "Kích thước " + s.size_name + " cho sản phẩm " + (s.Product != null ? s.Product.name : ""),
                    ProductCount = 1,
                    Status = s.status == "Active" || s.status == "Đang bán" ? "Đang sử dụng" : "Ngừng sử dụng",
                    Price = s.price_add
                })
                .ToList();

            ViewBag.SizesList = sizes;
            return View();
        }


        // 2. CREATE (POST): Thêm size mới
        [HttpPost]
        public ActionResult Create(string product_id, string name, string letter, string description, string status)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(letter))
                {
                    return Json(new { success = false, message = "Vui lòng nhập ký hiệu size." });
                }

                if (string.IsNullOrEmpty(product_id))
                {
                    return Json(new { success = false, message = "Vui lòng chọn hoặc nhập mã sản phẩm áp dụng." });
                }

                string trimmedProdId = product_id.Trim();
                string trimmedLetter = letter.Trim().ToUpper();

                // Kiểm tra xem sản phẩm cha có tồn tại không để tránh lỗi khóa ngoại
                bool productExists = db.Products.Any(p => p.product_id.Trim() == trimmedProdId);
                if (!productExists)
                {
                    return Json(new { success = false, message = "Mã sản phẩm '" + trimmedProdId + "' không tồn tại trong hệ thống!" });
                }

                // Kiểm tra ràng buộc unique (product_id + size_name) theo yêu cầu đồ án
                bool sizeExists = db.ProductSizes.Any(s => s.product_id.Trim() == trimmedProdId && s.size_name.ToUpper() == trimmedLetter);
                if (sizeExists)
                {
                    return Json(new { success = false, message = "Sản phẩm này đã có size '" + trimmedLetter + "' rồi!" });
                }

                string newId = "SZ" + Guid.NewGuid().ToString("N").Substring(0, 7).ToUpper();

                var newSize = new ProductSize
                {
                    size_id = newId,
                    product_id = trimmedProdId, // Gán khóa ngoại product_id để không bị lỗi CSDL
                    size_name = trimmedLetter,
                    price_add = 0,
                    status = (status == "Đang sử dụng" || status == "Active") ? "Active" : "Inactive"
                };

                db.ProductSizes.Add(newSize);
                db.SaveChanges();

                return Json(new { success = true, message = "Thêm kích thước thành công!" });
            }
            catch (Exception ex)
            {
                // Trả về lỗi gốc từ CSDL (InnerException) nếu có để dễ debug
                string errorDetail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Lỗi CSDL: " + errorDetail });
            }
        }

        // 3. EDIT (POST): Cập nhật size
        [HttpPost]
        public ActionResult Edit(string id, string name, string letter, string description, string status)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return Json(new { success = false, message = "Mã size không hợp lệ." });
                }

                string trimmedId = id.Trim();
                var item = db.ProductSizes.FirstOrDefault(s => s.size_id.Trim() == trimmedId);
                if (item == null)
                {
                    return Json(new { success = false, model = "Không tìm thấy kích thước cần sửa." });
                }

                item.size_name = letter.Trim().ToUpper();
                item.status = status == "Đang sử dụng" ? "Active" : "Inactive";

                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { success = true, message = "Cập nhật kích thước thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // 4. DELETE (POST): Xóa size
        [HttpPost]
        public ActionResult Delete(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return Json(new { success = false, message = "Mã không hợp lệ." });
                }

                string trimmedId = id.Trim();
                var item = db.ProductSizes.FirstOrDefault(s => s.size_id.Trim() == trimmedId);
                if (item != null)
                {
                    db.ProductSizes.Remove(item);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Xóa kích thước thành công!" });
                }

                return Json(new { success = false, message = "Không tìm thấy kích thước." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}