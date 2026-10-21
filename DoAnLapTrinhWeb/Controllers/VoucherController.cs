using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models;

namespace DoAnLapTrinhWeb.Controllers
{
    public class VoucherController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // 1. TRANG DANH SÁCH MÃ GIẢM GIÁ
        // GET: Voucher/Index
        public ActionResult Index()
        {
            var now = DateTime.Now;
            var list = db.Vouchers.OrderByDescending(v => v.start_date).ToList();

            // Thống kê số liệu KPI hiển thị đầu trang
            ViewBag.TotalCount = list.Count;
            ViewBag.ActiveCount = list.Count(v => v.status == "Active" && v.start_date <= now && v.end_date >= now);
            ViewBag.UpcomingCount = list.Count(v => v.status == "Active" && v.start_date > now);
            ViewBag.UsedCount = db.Orders.Count(o => o.voucher_id != null);

            return View(list);
        }

        // 2. TẠO MÃ GIẢM GIÁ MỚI (GỌI TỪ MODAL AJAX)
        [HttpPost]
        public JsonResult Create(string code, string type, decimal value, decimal min_order_amount, DateTime start_date, DateTime end_date)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return Json(new { success = false, message = "Vui lòng nhập mã Voucher!" });
                }

                code = code.Trim().ToUpper();

                // Kiểm tra trùng mã code
                if (db.Vouchers.Any(v => v.code == code))
                {
                    return Json(new { success = false, message = "Mã voucher này đã tồn tại trong hệ thống!" });
                }

                // Kiểm tra ràng buộc ngày kết thúc phải lớn hơn ngày bắt đầu
                if (end_date <= start_date)
                {
                    return Json(new { success = false, message = "Ngày kết thúc phải lớn hơn ngày bắt đầu!" });
                }

                // Kiểm tra ràng buộc giá trị giảm
                if (value <= 0)
                {
                    return Json(new { success = false, message = "Giá trị giảm giá phải lớn hơn 0!" });
                }

                if (type == "Percent" && value > 100)
                {
                    return Json(new { success = false, message = "Giảm giá theo phần trăm không được vượt quá 100%!" });
                }

                // Tự sinh mã voucher_id tăng dần: VOU01, VOU02...
                string voucherId = GenerateNextVoucherId();

                var voucher = new Voucher
                {
                    voucher_id = voucherId,
                    code = code,
                    type = type, // 'Percent' hoặc 'Fixed'
                    value = value,
                    min_order_amount = min_order_amount,
                    start_date = start_date,
                    end_date = end_date,
                    status = "Active"
                };

                db.Vouchers.Add(voucher);
                db.SaveChanges();

                return Json(new { success = true, message = "Tạo mã khuyến mãi mới thành công!" });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Lỗi khi tạo voucher: " + inner });
            }
        }

        // 3. TẠM DỪNG / KÍCH HOẠT LẠI MÃ
        [HttpPost]
        public JsonResult ToggleStatus(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id)) return Json(new { success = false, message = "Mã không hợp lệ." });
                id = id.Trim();

                var voucher = db.Vouchers.Find(id);
                if (voucher == null) return Json(new { success = false, message = "Không tìm thấy voucher." });

                voucher.status = (voucher.status == "Active") ? "Inactive" : "Active";
                db.SaveChanges();

                string msg = voucher.status == "Active" ? "Đã kích hoạt lại mã ưu đãi!" : "Đã tạm dừng mã ưu đãi!";
                return Json(new { success = true, message = msg });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 4. XÓA MÃ VOUCHER
        [HttpPost]
        public JsonResult Delete(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id)) return Json(new { success = false, message = "Mã không hợp lệ." });
                id = id.Trim();

                var voucher = db.Vouchers.Find(id);
                if (voucher == null) return Json(new { success = false, message = "Không tìm thấy voucher." });

                // Kiểm tra ràng buộc: Voucher đã có khách dùng trong Order hoặc CustomerVoucher chưa
                bool isUsed = db.Orders.Any(o => o.voucher_id == id) || db.CustomerVouchers.Any(cv => cv.voucher_id == id);
                if (isUsed)
                {
                    // Chuyển sang Inactive thay vì xóa cứng
                    voucher.status = "Inactive";
                    db.SaveChanges();
                    return Json(new { success = true, message = "Voucher này đã có lịch sử áp dụng đơn hàng nên hệ thống đã tự động chuyển sang [Tạm ngừng] để bảo toàn dữ liệu sổ sách!" });
                }

                db.Vouchers.Remove(voucher);
                db.SaveChanges();
                return Json(new { success = true, message = "Đã xóa mã voucher thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
            }
        }

        // HÀM TỰ SINH MÃ VOUCHER: VOU01, VOU02...
        private string GenerateNextVoucherId()
        {
            var existingIds = db.Vouchers.Select(v => v.voucher_id).ToList();
            int max = 0;
            foreach (var raw in existingIds)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string s = raw.Trim();
                if (s.StartsWith("VOU", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(s.Substring(3), out int num))
                    {
                        if (num > max) max = num;
                    }
                }
            }
            return "VOU" + (max + 1).ToString("D2");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}