using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TV1.Data;

namespace TV1.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly TV1DbContext _db;

        public LoginModel(TV1DbContext db)
        {
            _db = db;
        }

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string Username { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        public string Password { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.Password))
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                return Page();
            }

            if (user.Status != "Active")
            {
                ModelState.AddModelError(string.Empty, "Tài khoản đang bị khóa hoặc không hoạt động.");
                return Page();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role?.RoleName ?? ""),
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync("TV1Cookie", new ClaimsPrincipal(identity));

            return RedirectToPage("/Profile/Index");
        }
    }
}
