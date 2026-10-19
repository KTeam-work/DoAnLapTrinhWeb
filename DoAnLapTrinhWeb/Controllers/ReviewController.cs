using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models;

namespace DoAnLapTrinhWeb.Controllers
{
    public class ReviewController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // GET: Review/Index
        public ActionResult Index()
        {
            try
            {
                // Lấy danh sách đánh giá từ CSDL kèm thông tin Product và Customer
                var reviews = db.ProductReviews
                    .Include(r => r.Product)
                    .Include(r => r.Customer)
                    .ToList()
                    .Select(r => new
                    {
                        Id = r.review_id != null ? r.review_id.Trim() : "",
                        Customer = r.Customer != null && !string.IsNullOrEmpty(r.Customer.full_name)
                                    ? r.Customer.full_name.Trim()
                                    : "Khách hàng ẩn danh",
                        Initial = r.Customer != null && !string.IsNullOrEmpty(r.Customer.full_name)
                                    ? string.Concat(r.Customer.full_name.Trim().Split(' ').Where(s => s.Length > 0).Select(s => s[0])).ToUpper()
                                    : "KH",
                        Product = r.Product != null && !string.IsNullOrEmpty(r.Product.name)
                                    ? r.Product.name.Trim()
                                    : "Sản phẩm",
                        Rating = r.rating,
                        Content = r.comment ?? "",
                        Date = r.created_at != default(DateTime)
                                    ? r.created_at.ToString("dd/MM/yyyy")
                                    : DateTime.Now.ToString("dd/MM/yyyy"),
                        Status = "Đã duyệt" // Do CSDL không có cột status, mặc định hiển thị là "Đã duyệt"
                    })
                    .ToList();

                ViewBag.ReviewsList = reviews;
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ReviewsList = new System.Collections.Generic.List<object>();
                ViewBag.Error = "Lỗi CSDL: " + ex.Message;
                return View();
            }
        }

        // Action Xóa đánh giá dùng cho AJAX ở View
        [HttpPost]
        public ActionResult Delete(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return Json(new { success = false, message = "Mã đánh giá không hợp lệ." });
                }

                string trimId = id.Trim();
                var review = db.ProductReviews.FirstOrDefault(r => r.review_id.Trim() == trimId);
                if (review != null)
                {
                    db.ProductReviews.Remove(review);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Xóa bài đánh giá thành công!" });
                }

                return Json(new { success = false, message = "Không tìm thấy đánh giá cần xóa." });
            }
            catch (Exception ex)
            {
                string err = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Lỗi CSDL: " + err });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}