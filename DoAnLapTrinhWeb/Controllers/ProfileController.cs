using System.Linq;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using DoAnLapTrinhWeb.Models;

namespace DoAnLapTrinhWeb.Controllers
{
    public class ProfileController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // Lấy user đang đăng nhập từ Session["UserId"] (giá trị là user_id kiểu string)
        private User GetCurrentUser()
        {
            if (Session["UserId"] == null) return null;
            string id = Session["UserId"].ToString();
            return db.Users.FirstOrDefault(x => x.user_id == id);
        }

        // ===== TRANG TÀI KHOẢN =====
        [HttpGet]
        public ActionResult Index()
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(string FullName, string Email, string PhoneNumber)
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");

            FullName = FullName?.Trim();
            Email = Email?.Trim();
            PhoneNumber = PhoneNumber?.Trim();

            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email))
            {
                TempData["ErrorMessage"] = "Họ tên và Email không được để trống.";
                return RedirectToAction("Index");
            }

            // Kiểm tra trùng lặp email với tài khoản khác
            bool emailExists = db.Users.Any(u => u.email == Email && u.user_id != user.user_id);
            if (emailExists)
            {
                TempData["ErrorMessage"] = "Email đã tồn tại trong hệ thống.";
                return RedirectToAction("Index");
            }

            user.full_name = FullName;
            user.email = Email;
            user.phone = PhoneNumber;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Index");
        }

        // ===== ĐỔI MẬT KHẨU =====
        [HttpGet]
        public ActionResult ChangePassword()
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Truyền mật khẩu hiện tại trong DB ra View để hiển thị
            ViewBag.CurrentPassword = user.password;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string CurrentPassword, string NewPassword, string ConfirmNewPassword)
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Giữ lại mật khẩu để không bị mất khi load lại form do lỗi
            ViewBag.CurrentPassword = CurrentPassword;

            if (string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(NewPassword))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin.");
                return View();
            }

            if (NewPassword != ConfirmNewPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
                return View();
            }

            // Kiểm tra mật khẩu mới bằng Regex
            string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";
            if (!Regex.IsMatch(NewPassword, passwordPattern))
            {
                ModelState.AddModelError("", "Mật khẩu mới phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");
                return View();
            }

            // Kiểm tra mật khẩu hiện tại
            if (user.password != CurrentPassword)
            {
                ModelState.AddModelError("", "Mật khẩu hiện tại không đúng.");
                return View();
            }

            // Lưu mật khẩu mới
            user.password = NewPassword;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}