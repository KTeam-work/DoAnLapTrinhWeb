using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models;

namespace DoAnLapTrinhWeb.Controllers
{
    public class EmployeeController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // 1. DANH SÁCH NHÂN VIÊN
        // GET: Employee/Index
        public ActionResult Index()
        {
            var employees = db.Users
                .Include(u => u.Role)
                .OrderByDescending(u => u.created_at)
                .ToList();

            ViewBag.Roles = db.Roles.ToList();
            ViewBag.TotalCount = employees.Count;

            return View(employees);
        }

        // 2. THÊM MỚI NHÂN VIÊN (TỪ MODAL TRANG INDEX)
        [HttpPost]
        public ActionResult Create(string FullName, string RoleId, string Phone, string Email, string Username, string Password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(RoleId))
                {
                    TempData["Error"] = "Vui lòng nhập đầy đủ thông tin bắt buộc!";
                    return RedirectToAction("Index");
                }

                // Tự động tạo Username từ Email nếu để trống (VD: an.nguyen@lumitea.com -> an.nguyen)
                string finalUsername = !string.IsNullOrWhiteSpace(Username)
                    ? Username.Trim()
                    : (!string.IsNullOrWhiteSpace(Email) ? Email.Split('@')[0].Trim() : "user" + DateTime.Now.ToString("ddHHmm"));

                // Kiểm tra trùng username
                if (db.Users.Any(u => u.username == finalUsername))
                {
                    finalUsername = finalUsername + new Random().Next(10, 99);
                }

                // Tự động tạo mật khẩu mặc định "123456" nếu để trống
                string finalPassword = !string.IsNullOrWhiteSpace(Password) ? Password.Trim() : "123456";

                string newUserId = GenerateNextUserId();

                var newUser = new User
                {
                    user_id = newUserId,
                    role_id = RoleId.Trim(),
                    username = finalUsername,
                    password = finalPassword,
                    full_name = FullName.Trim(),
                    email = !string.IsNullOrWhiteSpace(Email) ? Email.Trim() : $"{finalUsername}@lumitea.com",
                    phone = !string.IsNullOrWhiteSpace(Phone) ? Phone.Trim() : null,
                    status = "Active",
                    created_at = DateTime.Now
                };

                db.Users.Add(newUser);
                db.SaveChanges();

                TempData["Success"] = $"Đã thêm nhân viên {FullName} thành công (Tài khoản: {finalUsername} / Mật khẩu: {finalPassword})!";
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.InnerException?.Message ?? ex.Message;
                TempData["Error"] = "Lỗi khi thêm nhân viên: " + inner;
            }

            return RedirectToAction("Index");
        }

        // 3. BẬT / TẮT TRẠNG THÁI HOẠT ĐỘNG (SWITCH TRÊN BẢNG)
        [HttpPost]
        public JsonResult ToggleStatus(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id)) return Json(new { success = false, message = "Mã không hợp lệ." });
                id = id.Trim();

                var user = db.Users.Find(id);
                if (user == null) return Json(new { success = false, message = "Không tìm thấy nhân viên." });

                // Tránh trường hợp tự khóa tài khoản Admin đang đăng nhập
                string currentUserId = Session["UserId"] != null ? Session["UserId"].ToString().Trim() : "";
                if (user.user_id.Trim() == currentUserId)
                {
                    return Json(new { success = false, message = "Bạn không thể tự khóa tài khoản đang đăng nhập!" });
                }

                user.status = (user.status == "Active") ? "Locked" : "Active";
                db.SaveChanges();

                return Json(new { success = true, is_active = (user.status == "Active"), message = "Đã cập nhật trạng thái!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 4. XÓA NHÂN VIÊN
        [HttpPost]
        public JsonResult Delete(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id)) return Json(new { success = false, message = "Mã không hợp lệ." });
                id = id.Trim();

                var user = db.Users.Find(id);
                if (user == null) return Json(new { success = false, message = "Không tìm thấy nhân viên." });

                // Kiểm tra xem nhân viên đã từng tạo đơn hàng, nhập kho hoặc phân ca chưa
                bool hasHistory = db.Orders.Any(o => o.user_id == id)
                               || db.PurchaseOrders.Any(po => po.user_id == id)
                               || db.StaffShifts.Any(ss => ss.user_id == id);

                if (hasHistory)
                {
                    // Chuyển sang khóa tài khoản thay vì xóa cứng để không hỏng lịch sử
                    user.status = "Locked";
                    db.SaveChanges();
                    return Json(new { success = true, message = "Nhân viên này đã có dữ liệu phân ca hoặc lịch sử vận hành, hệ thống đã tự động chuyển sang [Tạm khóa]!" });
                }

                db.Users.Remove(user);
                db.SaveChanges();
                return Json(new { success = true, message = "Đã xóa tài khoản nhân viên thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // HÀM TỰ SINH MÃ USER TĂNG DẦN: U01, U02, U03... CHUẨN NCHAR(10)
        private string GenerateNextUserId()
        {
            var existingIds = db.Users.Select(u => u.user_id).ToList();
            int max = 0;
            foreach (var raw in existingIds)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string s = raw.Trim();
                if (s.StartsWith("U", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(s.Substring(1), out int num))
                    {
                        if (num > max) max = num;
                    }
                }
            }
            return "U" + (max + 1).ToString("D2");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}