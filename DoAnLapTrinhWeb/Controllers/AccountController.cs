using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using DoAnLapTrinhWeb.Models;

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

            if (user.status != null && user.status.Trim() != "Active")
            {
                ModelState.AddModelError("", "Tài khoản đã bị khóa hoặc chưa kích hoạt.");
                return View();
            }

            // Lưu session và cắt khoảng trắng đệm của NCHAR(10)
            Session["UserId"] = user.user_id.Trim();
            Session["Username"] = user.username.Trim();
            Session["FullName"] = user.full_name;

            string roleId = (user.role_id ?? "").Trim();
            Session["UserRole"] = roleId;

            // Điều hướng theo database: R01 = Admin, R02 = Staff, R03 = Customer
            if (roleId == "R01")
            {
                return RedirectToAction("Index", "Product"); // Admin vào quản lý món
            }
            else if (roleId == "R02")
            {
                return RedirectToAction("Index", "Shift");   // Staff vào ca làm việc
            }
            else
            {
                return RedirectToAction("Index", "Home");    // Khách hàng vào trang chủ
            }
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
                user_id = ("U" + Guid.NewGuid().ToString("N")).Substring(0, 10),
                full_name = FullName,
                username = Username,
                password = Password,
                email = Email,
                phone = Phone,
                role_id = "R03", 
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
        public async Task<ActionResult> ForgotPassword(string Email)
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ModelState.AddModelError("", "Vui lòng nhập email.");
                return View();
            }

            string cleanEmail = Email.Trim();
            var user = db.Users.FirstOrDefault(x => x.email == cleanEmail);
            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản nào khớp với email này.");
                return View();
            }

            try
            {
                // 1. Tạo mật khẩu mới ngẫu nhiên
                string randomCode = new Random().Next(100000, 999999).ToString();
                string newTempPassword = "Pass@" + randomCode;

                // 2. Cập nhật mật khẩu vào DB
                user.password = newTempPassword;
                db.SaveChanges();

                // 3. Gọi REST API bên ngoài (Resend API)
                string apiKey = "Your_API_KEY"; // Thay bằng API Key của bạn
                string apiUrl = "https://api.resend.com/emails";

                var payload = new
                {
                    from = "onboarding@resend.dev",
                    to = new string[] { cleanEmail },
                    subject = "Cấp lại mật khẩu tài khoản Quán Trà Sữa",
                    html = $@"
                        <h3>Xin chào {user.full_name},</h3>
                        <p>Mật khẩu tạm thời cho tài khoản <b>{user.username}</b> của bạn là:</p>
                        <h2 style='color: #8A4B29;'>{newTempPassword}</h2>
                        <p>Vui lòng đăng nhập và đổi lại mật khẩu để bảo vệ tài khoản.</p>"
                };

                string jsonContent = new JavaScriptSerializer().Serialize(payload);

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(apiUrl, httpContent);
                    if (response.IsSuccessStatusCode)
                    {
                        ViewBag.EmailSent = true;
                        ViewBag.SuccessMessage = "Mật khẩu mới đã được gửi qua email!";
                        return View();
                    }
                    else
                    {
                        string errorResponse = await response.Content.ReadAsStringAsync();
                        ModelState.AddModelError("", "Lỗi từ API Email: " + errorResponse);
                        return View();
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                return View();
            }
        }
    }
}