using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TV1.Data;
using TV1.Models;

namespace TV1.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly TV1DbContext _db;

        public RegisterModel(TV1DbContext db)
        {
            _db = db;
        }

        [BindProperty]
        [Required]
        public string FullName { get; set; }

        [BindProperty]
        [Required]
        public string Username { get; set; }

        [BindProperty]
        [Required, EmailAddress]
        public string Email { get; set; }

        [BindProperty]
        [Phone]
        public string Phone { get; set; }

        [BindProperty]
        [Required, MinLength(6)]
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

            if (await _db.Users.AnyAsync(u => u.Username == Username))
            {
                ModelState.AddModelError(nameof(Username), "Tên đăng nhập đã tồn tại.");
                return Page();
            }

            if (await _db.Users.AnyAsync(u => u.Email == Email))
            {
                ModelState.AddModelError(nameof(Email), "Email đã được sử dụng.");
                return Page();
            }

            var user = new User
            {
                UserId = "U" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                RoleId = "R002", // Member mac dinh
                Username = Username,
                Password = BCrypt.Net.BCrypt.HashPassword(Password),
                FullName = FullName,
                Email = Email,
                Phone = Phone,
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Account/Login");
        }
    }
}
