using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models;

namespace DoAnLapTrinhWeb.Controllers
{
    public class CustomerController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // 1. DANH SÁCH KHÁCH HÀNG (DẠNG GRID / COMPACT)
        // GET: Customer/Index
        public ActionResult Index()
        {
            var customers = db.Customers
                .Include(c => c.User)
                .Include(c => c.Orders)
                .OrderByDescending(c => c.created_at)
                .ToList();

            int totalCustomers = customers.Count;
            int vipCount = customers.Count(c => c.loyalty_point >= 1000 || (c.Orders.Where(o => o.status == "Completed").Sum(o => (decimal?)o.total_amount) ?? 0) >= 10000000);

            decimal totalRevenue = customers.SelectMany(c => c.Orders).Where(o => o.status == "Completed").Sum(o => (decimal?)o.total_amount) ?? 0;
            decimal avgSpent = totalCustomers > 0 ? (totalRevenue / totalCustomers) : 0;

            int lockedCount = customers.Count(c => c.User != null && c.User.status == "Locked");

            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.VipCount = vipCount;
            ViewBag.AvgSpent = avgSpent;
            ViewBag.LockedCount = lockedCount;

            return View(customers);
        }

        // 2. CHI TIẾT HỒ SƠ 360° CỦA KHÁCH HÀNG
        // GET: Customer/Details/CUS01
        public ActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

            id = id.Trim();
            var customer = db.Customers
                .Include(c => c.User)
                .Include(c => c.Orders)
                .Include(c => c.CustomerVouchers.Select(cv => cv.Voucher))
                .FirstOrDefault(c => c.customer_id == id);

            if (customer == null) return RedirectToAction("Index");

            return View(customer);
        }

        // 3. API: LẤY DANH SÁCH VOUCHER ĐANG HOẠT ĐỘNG (CHO MODAL TẶNG VOUCHER)
        [HttpGet]
        public JsonResult GetActiveVouchers()
        {
            try
            {
                var now = DateTime.Now;
                var list = db.Vouchers
                    .Where(v => v.status == "Active" && v.end_date >= now)
                    .AsEnumerable()
                    .Select(v => new
                    {
                        id = v.voucher_id.Trim(),
                        code = v.code.Trim(),
                        desc = (v.type == "Percent" ? $"Giảm {v.value:0.##}%" : $"Giảm {v.value:N0}đ") + $" (Đơn tối thiểu {v.min_order_amount:N0}đ)"
                    })
                    .ToList();

                return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // 4. THÊM KHÁCH HÀNG MỚI (MODAL TRANG INDEX)
        [HttpPost]
        public JsonResult AddCustomer(string name, string phone, string email, string rank)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
                {
                    return Json(new { success = false, message = "Vui lòng nhập họ tên và số điện thoại!" });
                }

                phone = phone.Trim();
                if (db.Customers.Any(c => c.phone == phone))
                {
                    return Json(new { success = false, message = "Số điện thoại này đã được đăng ký!" });
                }

                int initialPoints = 0;
                if (rank == "Gold") initialPoints = 500;
                else if (rank == "VIP") initialPoints = 1000;

                string newCustomerId = GenerateNextCustomerId();

                var customer = new Customer
                {
                    customer_id = newCustomerId,
                    user_id = null,
                    full_name = name.Trim(),
                    phone = phone,
                    email = !string.IsNullOrWhiteSpace(email) ? email.Trim() : null,
                    loyalty_point = initialPoints,
                    created_at = DateTime.Now
                };

                db.Customers.Add(customer);
                db.SaveChanges();

                return Json(new { success = true, message = "Thêm khách hàng thành công!" });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Lỗi khi thêm: " + inner });
            }
        }

        // 5. CẬP NHẬT THÔNG TIN HỒ SƠ (ĐÃ BỔ SUNG THAM SỐ PHONE)
        [HttpPost]
        public JsonResult UpdateProfile(string customerId, string fullName, string phone, string email, DateTime? birthday)
        {
            try
            {
                if (string.IsNullOrEmpty(customerId)) return Json(new { success = false, message = "Mã không hợp lệ." });
                customerId = customerId.Trim();

                var customer = db.Customers.Find(customerId);
                if (customer == null) return Json(new { success = false, message = "Không tìm thấy khách hàng." });

                if (!string.IsNullOrWhiteSpace(fullName)) customer.full_name = fullName.Trim();
                customer.phone = !string.IsNullOrWhiteSpace(phone) ? phone.Trim() : null;
                customer.email = !string.IsNullOrWhiteSpace(email) ? email.Trim() : null;
                customer.birthday = birthday;

                db.SaveChanges();
                return Json(new { success = true, message = "Cập nhật hồ sơ khách hàng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 6. CỘNG ĐIỂM TÍCH LŨY THƯỞNG (ĐÃ ĐỔI TÊN THÀNH AddLoyaltyPoints & NHẬN customerId)
        [HttpPost]
        public JsonResult AddLoyaltyPoints(string customerId, int points)
        {
            try
            {
                if (string.IsNullOrEmpty(customerId) || points <= 0)
                    return Json(new { success = false, message = "Dữ liệu điểm không hợp lệ." });

                customerId = customerId.Trim();
                var customer = db.Customers.Find(customerId);
                if (customer == null) return Json(new { success = false, message = "Không tìm thấy khách hàng." });

                customer.loyalty_point += points;
                db.SaveChanges();

                return Json(new { success = true, newPoints = customer.loyalty_point, message = $"Đã cộng {points} điểm thưởng cho khách hàng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 7. KHÓA / MỞ KHÓA TÀI KHOẢN KHÁCH HÀNG (ĐÃ NHẬN customerId)
        [HttpPost]
        public JsonResult ToggleLock(string customerId)
        {
            try
            {
                if (string.IsNullOrEmpty(customerId)) return Json(new { success = false, message = "Mã không hợp lệ." });

                customerId = customerId.Trim();
                var customer = db.Customers.Include(c => c.User).FirstOrDefault(c => c.customer_id.Trim() == customerId);
                if (customer == null) return Json(new { success = false, message = "Không tìm thấy khách hàng." });

                if (customer.User == null)
                {
                    return Json(new { success = false, message = "Khách hàng này chưa liên kết tài khoản đăng nhập hệ thống nên không thể khóa." });
                }

                customer.User.status = (customer.User.status == "Locked") ? "Active" : "Locked";
                db.SaveChanges();

                string msg = (customer.User.status == "Locked") ? "Đã khóa tài khoản khách hàng!" : "Đã mở khóa tài khoản khách hàng!";
                return Json(new { success = true, message = msg, status = customer.User.status });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private static bool idMatch(string colId, string targetId)
        {
            return colId != null && colId.Trim() == targetId;
        }

        // 8. TẶNG VOUCHER VÀO VÍ KHÁCH HÀNG
        [HttpPost]
        public JsonResult GiftVoucher(string customerId, string voucherId)
        {
            try
            {
                if (string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(voucherId))
                    return Json(new { success = false, message = "Thiếu thông tin khách hàng hoặc mã voucher." });

                customerId = customerId.Trim();
                voucherId = voucherId.Trim();

                // Tạo mã khóa chính CustomerVouchers: "CV" + 8 ký tự = đúng chuẩn NCHAR(10)
                string newCvId = "CV" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

                var cv = new CustomerVoucher
                {
                    customer_voucher_id = newCvId,
                    customer_id = customerId,
                    voucher_id = voucherId,
                    used_at = null,
                    status = "Unused"
                };

                db.CustomerVouchers.Add(cv);
                db.SaveChanges();

                return Json(new { success = true, message = "Đã gửi tặng voucher vào ví khách hàng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // HÀM TỰ SINH MÃ KHÁCH HÀNG: CUS01, CUS02...
        private string GenerateNextCustomerId()
        {
            var existingIds = db.Customers.Select(c => c.customer_id).ToList();
            int max = 0;
            foreach (var raw in existingIds)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string s = raw.Trim();
                if (s.StartsWith("CUS", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(s.Substring(3), out int num))
                    {
                        if (num > max) max = num;
                    }
                }
            }
            return "CUS" + (max + 1).ToString("D2");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}