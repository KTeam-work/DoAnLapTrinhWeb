using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TV1.Data;

namespace TV1.Pages.Profile
{
    [Authorize(AuthenticationSchemes = "TV1Cookie")]
    public class IndexModel : PageModel
    {
        private readonly TV1DbContext _db;

        public IndexModel(TV1DbContext db)
        {
            _db = db;
        }

        public string FullName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string RoleName { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user != null)
            {
                FullName = user.FullName;
                Username = user.Username;
                Email = user.Email;
                Phone = user.Phone;
                RoleName = user.Role?.RoleName;
            }
        }
    }
}
