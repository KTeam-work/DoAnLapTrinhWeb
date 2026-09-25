using System.Web.Mvc;
using System.Web.Security;
using DoAnLapTrinhWeb.Controllers;
using DoAnLapTrinhWeb.Models;

namespace DoAnLapTrinhWeb.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/Register
        [HttpGet]
        public ActionResult Register()
        {
            if (Session["UserId"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (UserRepository.UsernameExists(model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập này đã được sử dụng.");
            }

            if (UserRepository.EmailExists(model.Email))
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var newUser = new User
            {
                FullName = model.FullName,
                Username = model.Username,
                Email = model.Email,
                PhoneNumber = model.Phone
            };

            UserRepository.Add(newUser, model.Password);

            // Tự động đăng nhập ngay sau khi đăng ký thành công
            Session["UserId"] = newUser.Id;
            Session["UserName"] = newUser.FullName;

            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Chào mừng bạn đến với TV1.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Login
        [HttpGet]
        public ActionResult Login()
        {
            if (Session["UserId"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = UserRepository.FindByUsernameOrEmail(model.UsernameOrEmail);

            if (user == null || !UserRepository.VerifyPassword(user, model.Password))
            {
                ModelState.AddModelError("", "Tên đăng nhập/email hoặc mật khẩu không đúng.");
                return View(model);
            }

            Session["UserId"] = user.Id;
            Session["UserName"] = user.FullName;

            if (model.RememberMe)
            {
                FormsAuthentication.SetAuthCookie(user.Username, true);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Ghi chú: project chưa cấu hình dịch vụ gửi email thật.
            // Ở đây chỉ xác nhận là đã "gửi" (không tiết lộ email có tồn tại hay không, để tránh dò quét tài khoản).
            model.EmailSent = true;
            return View(model);
        }
    }
}
