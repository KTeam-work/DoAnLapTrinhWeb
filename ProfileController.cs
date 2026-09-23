using System.Web.Mvc;
using QuanLyTraSua.Models;

namespace QuanLyTraSua.Controllers
{
    public class ProfileController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Chưa đăng nhập thì không cho xem trang hồ sơ
            if (Session["UserId"] == null)
            {
                filterContext.Result = RedirectToAction("Login", "Account");
                return;
            }
            base.OnActionExecuting(filterContext);
        }

        private User CurrentUser
        {
            get { return UserRepository.FindById((int)Session["UserId"]); }
        }

        // GET: /Profile/Index
        [HttpGet]
        public ActionResult Index()
        {
            var user = CurrentUser;
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return View(user);
        }

        // POST: /Profile/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(UpdateProfileViewModel model)
        {
            var user = CurrentUser;
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin đã nhập.";
                return RedirectToAction("Index");
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            Session["UserName"] = user.FullName;
            TempData["SuccessMessage"] = "Đã cập nhật thông tin cá nhân.";
            return RedirectToAction("Index");
        }

        // GET: /Profile/ChangePassword
        [HttpGet]
        public ActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: /Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            var user = CurrentUser;
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!UserRepository.VerifyPassword(user, model.CurrentPassword))
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                return View(model);
            }

            UserRepository.SetPassword(user, model.NewPassword);
            TempData["SuccessMessage"] = "Đã đổi mật khẩu thành công.";
            return RedirectToAction("Index");
        }
    }
}
