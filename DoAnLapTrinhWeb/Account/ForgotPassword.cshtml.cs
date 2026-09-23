using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TV1.Data;

namespace TV1.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly TV1DbContext _db;

        public ForgotPasswordModel(TV1DbContext db)
        {
            _db = db;
        }

        [BindProperty]
        [Required, EmailAddress]
        public string Email { get; set; }

        public bool EmailSent { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == Email);

            // Khong tiet lo email co ton tai hay khong (tranh do tham tai khoan)
            if (user != null)
            {
                // TODO: sinh token dat lai mat khau, luu vao bang rieng (vd PasswordResetTokens)
                // va gui email chua link dat lai mat khau qua dich vu SMTP/SendGrid...
            }

            EmailSent = true;
            return Page();
        }
    }
}
