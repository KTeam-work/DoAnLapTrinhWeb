using DoAnLapTrinhWeb.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace DoAnLapTrinhWeb.Controllers
{
    public class AccountController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        [HttpGet]
        public ActionResult Login()
        {
            if (Session["UserId"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string Username, string Password)
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
                return View();
            }

            var user = db.Users.FirstOrDefault(x => x.username == Username && x.password == Password);
            if (user == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View();
            }

            if (user.status != "Active")
            {
                ModelState.AddModelError("", "Tài khoản đã bị khóa hoặc chưa kích hoạt.");
                return View();
            }

            Session["UserId"] = user.user_id;
            Session["Username"] = user.username;
            Session["FullName"] = user.full_name;

            return RedirectToAction("Index", "Home");
        }

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string FullName, string Username, string Email, string Phone, string Password, string ConfirmPassword)
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(FullName))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (string.IsNullOrEmpty(Email) || !Regex.IsMatch(Email, emailPattern))
            {
                ViewBag.Error = "Địa chỉ email không hợp lệ (ví dụ: ban@gmail.com).";
                return View();
            }

            if (Password != ConfirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";
            if (!Regex.IsMatch(Password, passwordPattern))
            {
                ViewBag.Error = "Mật khẩu phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.";
                return View();
            }

            if (db.Users.Any(x => x.username == Username))
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.";
                return View();
            }

            if (db.Users.Any(x => x.email == Email))
            {
                ViewBag.Error = "Email này đã được đăng ký.";
                return View();
            }

            User newUser = new User()
            {
                user_id = Guid.NewGuid().ToString("N").Substring(0, 10),
                full_name = FullName,
                username = Username,
                password = Password,
                email = Email,
                phone = Phone,
                role_id = "R02",
                status = "Active",
                created_at = DateTime.Now
            };

            db.Users.Add(newUser);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string Email)
        {
            if (string.IsNullOrEmpty(Email))
            {
                ModelState.AddModelError("", "Vui lòng nhập email.");
                return View();
            }

            ViewBag.EmailSent = true;
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}