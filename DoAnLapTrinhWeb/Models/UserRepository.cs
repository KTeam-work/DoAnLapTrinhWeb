using System;
using System.Collections.Generic;
using System.Linq;

namespace DoAnLapTrinhWeb.Models
{
    /// <summary>
    /// Kho lưu người dùng tạm thời trong bộ nhớ (static List, có khóa lock để an toàn khi
    /// nhiều request cùng truy cập). Dữ liệu sẽ mất khi ứng dụng khởi động lại vì project này
    /// chưa cấu hình cơ sở dữ liệu (Entity Framework / SQL Server).
    /// Mật khẩu được lưu dạng chuỗi thường theo yêu cầu, không mã hoá.
    /// </summary>
    public static class UserRepository
    {
        private static readonly object _lock = new object();
        private static readonly List<User> _users = new List<User>();
        private static int _nextId = 1;

        static UserRepository()
        {
            // Tài khoản mẫu để test đăng nhập ngay: username "test", mật khẩu "123456"
            Add(new User
            {
                FullName = "Nguyễn A",
                Username = "test",
                Email = "test@gmail.com",
                PhoneNumber = "0909123456"
            }, "123456");
        }

        public static User Add(User user, string plainPassword)
        {
            lock (_lock)
            {
                user.Id = _nextId++;
                user.CreatedAt = DateTime.Now;
                user.Password = plainPassword;
                _users.Add(user);
                return user;
            }
        }

        public static User FindById(int id)
        {
            lock (_lock)
            {
                return _users.FirstOrDefault(u => u.Id == id);
            }
        }

        public static User FindByUsernameOrEmail(string usernameOrEmail)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail)) return null;
            lock (_lock)
            {
                return _users.FirstOrDefault(u =>
                    string.Equals(u.Username, usernameOrEmail, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.Email, usernameOrEmail, StringComparison.OrdinalIgnoreCase));
            }
        }

        public static bool UsernameExists(string username)
        {
            lock (_lock)
            {
                return _users.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
            }
        }

        public static bool EmailExists(string email)
        {
            lock (_lock)
            {
                return _users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            }
        }

        public static bool VerifyPassword(User user, string plainPassword)
        {
            if (user == null) return false;
            return user.Password == plainPassword;
        }

        public static void SetPassword(User user, string newPlainPassword)
        {
            lock (_lock)
            {
                user.Password = newPlainPassword;
            }
        }
    }
}
