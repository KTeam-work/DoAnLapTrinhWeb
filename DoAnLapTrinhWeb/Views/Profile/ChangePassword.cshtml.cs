using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TV1.Data;

namespace TV1.Pages.Profile
{
    [Authorize(AuthenticationSchemes = "TV1Cookie")]
    public class ChangePasswordModel : PageModel
    {
        private readonly TV1DbContext _db;

        public ChangePasswordModel(TV1DbContext db)
        {
            _db = db;
        }

        [BindProperty]
        [Required]
        public string OldPassword { get; set; }

        [BindProperty]
        [Required, MinLength(6)]
        public string NewPassword { get; set; }

        [BindProperty]
        [Required, Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || !BCrypt.Net.BCrypt.Verify(OldPassword, user.Password))
            {
                ModelState.AddModelError(nameof(OldPassword), "Mật khẩu hiện tại không đúng.");
                return Page();
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Profile/Index");
        }
    }
}
