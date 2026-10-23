using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models;

namespace DoAnLapTrinhWeb.Controllers
{
    public class ShiftController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // 1. TRẠM ĐIỂM DANH HÔM NAY
        // GET: Shift/Index
        public ActionResult Index(DateTime? date)
        {
            DateTime filterDate = (date ?? DateTime.Today).Date;

            // Lấy danh sách những nhân viên ĐÃ ĐƯỢC XẾP CA ngày hôm nay
            var todayShifts = db.StaffShifts
                .Include(s => s.User)
                .Include(s => s.User.Role)
                .Include(s => s.Shift)
                .Where(s => DbFunctions.TruncateTime(s.work_date) == filterDate)
                .OrderBy(s => s.Shift.start_time)
                .ToList();

            // Danh sách mã nhân viên đã điểm danh hôm nay (lưu trong Session phiên làm việc)
            var checkedInIds = Session["CheckedInStaffToday"] as List<string> ?? new List<string>();

            ViewBag.CheckedInIds = checkedInIds;
            ViewBag.SelectedDate = filterDate.ToString("yyyy-MM-dd");

            return View(todayShifts);
        }

        // 2. XÁC NHẬN ĐIỂM DANH VÀO CA TRỰC BẰNG MÃ PIN
        [HttpPost]
        public ActionResult CheckIn(string EmployeeCode, string PinCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EmployeeCode))
                {
                    TempData["Error"] = "Vui lòng chọn nhân viên có ca trực hôm nay!";
                    return RedirectToAction("Index");
                }

                EmployeeCode = EmployeeCode.Trim();
                DateTime today = DateTime.Today;

                // CHẶN NGAY: Kiểm tra nhân viên này có lịch vào ca hôm nay không
                var shiftToday = db.StaffShifts
                    .Include(s => s.User)
                    .Include(s => s.Shift)
                    .FirstOrDefault(s => s.user_id.Trim() == EmployeeCode && DbFunctions.TruncateTime(s.work_date) == today);

                if (shiftToday == null)
                {
                    TempData["Error"] = "Nhân viên này không có lịch trực ca hôm nay, không thể điểm danh!";
                    return RedirectToAction("Index");
                }

                var user = shiftToday.User;

                // Kiểm tra mã PIN 4 số (chấp nhận: 1234, mật khẩu tài khoản, hoặc 4 số cuối SĐT)
                string phone = user.phone ?? "";
                string last4 = phone.Length >= 4 ? phone.Substring(phone.Length - 4) : "";
                bool isPinValid = (PinCode == "1234") || (PinCode == last4) || (user.password == PinCode);

                if (!isPinValid)
                {
                    TempData["Error"] = "Mã PIN xác nhận không chính xác! (Mặc định: 1234 hoặc 4 số cuối SĐT)";
                    return RedirectToAction("Index");
                }

                // Ghi nhận trạng thái ĐÃ ĐIỂM DANH
                var checkedInList = Session["CheckedInStaffToday"] as List<string> ?? new List<string>();
                if (!checkedInList.Contains(EmployeeCode))
                {
                    checkedInList.Add(EmployeeCode);
                    Session["CheckedInStaffToday"] = checkedInList;
                }

                TempData["Success"] = $"Điểm danh thành công! Đã xác nhận nhân viên {user.full_name} vào ca [{shiftToday.Shift.shift_name}].";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi điểm danh: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // 3. KẾT THÚC / RỜI CA LÀM VIỆC (RA CA)
        [HttpPost]
        public JsonResult CheckOut(string employeeCode)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeCode)) return Json(new { success = false, message = "Mã không hợp lệ." });
                employeeCode = employeeCode.Trim();

                var checkedInList = Session["CheckedInStaffToday"] as List<string> ?? new List<string>();
                if (checkedInList.Contains(employeeCode))
                {
                    checkedInList.Remove(employeeCode);
                    Session["CheckedInStaffToday"] = checkedInList;
                }

                return Json(new { success = true, message = "Đã xác nhận kết thúc ca làm việc (Ra ca) thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 4. BẢNG PHÂN CA 7 NGÀY TUẦN
        // GET: Shift/Schedule
        public ActionResult Schedule(int? weekOffset)
        {
            int offset = weekOffset ?? 0;
            DateTime baseDate = DateTime.Today.AddDays(offset * 7);

            int diff = (7 + (baseDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime monday = baseDate.AddDays(-1 * diff).Date;
            DateTime sunday = monday.AddDays(6).Date;

            var shiftsInWeek = db.StaffShifts
                .Include(s => s.User)
                .Include(s => s.User.Role)
                .Include(s => s.Shift)
                .Where(s => DbFunctions.TruncateTime(s.work_date) >= monday && DbFunctions.TruncateTime(s.work_date) <= sunday)
                .ToList();

            ViewBag.StartOfWeek = monday;
            ViewBag.EndOfWeek = sunday;
            ViewBag.WeekOffset = offset;
            ViewBag.Shifts = db.Shifts.OrderBy(s => s.start_time).ToList();
            ViewBag.Users = db.Users.Where(u => u.status == "Active").Include(u => u.Role).ToList();

            return View(shiftsInWeek);
        }

        // 5. LƯU PHÂN CA (GỌI TỪ MODAL SCHEDULE)
        [HttpPost]
        public ActionResult AssignShift(string UserId, string ShiftId, DateTime WorkDate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(ShiftId))
                {
                    TempData["Error"] = "Vui lòng chọn nhân viên và ca làm việc!";
                    return RedirectToAction("Schedule");
                }

                UserId = UserId.Trim();
                ShiftId = ShiftId.Trim();
                DateTime targetDate = WorkDate.Date;

                // Ràng buộc UQ_StaffShifts_UserDate: 1 nhân viên chỉ trực 1 ca/ngày
                bool isExisted = db.StaffShifts.Any(s => s.user_id.Trim() == UserId && DbFunctions.TruncateTime(s.work_date) == targetDate);
                if (isExisted)
                {
                    TempData["Error"] = $"Nhân viên này đã có ca trực trong ngày {targetDate:dd/MM/yyyy} rồi!";
                    return RedirectToAction("Schedule");
                }

                var newStaffShift = new StaffShift
                {
                    staff_shift_id = GenerateNextStaffShiftId(),
                    user_id = UserId,
                    shift_id = ShiftId,
                    work_date = targetDate,
                    created_at = DateTime.Now
                };

                db.StaffShifts.Add(newStaffShift);
                db.SaveChanges();

                TempData["Success"] = "Đã xếp ca làm việc cho nhân viên thành công!";
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.InnerException?.Message ?? ex.Message;
                TempData["Error"] = "Lỗi khi phân ca: " + inner;
            }

            return RedirectToAction("Schedule");
        }

        // 6. HỦY PHÂN CA
        [HttpPost]
        public JsonResult DeleteShift(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id)) return Json(new { success = false, message = "Mã không hợp lệ." });
                id = id.Trim();

                var item = db.StaffShifts.Find(id);
                if (item == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu ca trực." });

                // Đồng thời gỡ khỏi danh sách đã điểm danh nếu có
                var checkedInList = Session["CheckedInStaffToday"] as List<string> ?? new List<string>();
                if (checkedInList.Contains(item.user_id.Trim()))
                {
                    checkedInList.Remove(item.user_id.Trim());
                    Session["CheckedInStaffToday"] = checkedInList;
                }

                db.StaffShifts.Remove(item);
                db.SaveChanges();

                return Json(new { success = true, message = "Đã hủy ca trực thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GenerateNextStaffShiftId()
        {
            var existingIds = db.StaffShifts.Select(s => s.staff_shift_id).ToList();
            int max = 0;
            foreach (var raw in existingIds)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string s = raw.Trim();
                if (s.StartsWith("SS", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(s.Substring(2), out int num))
                    {
                        if (num > max) max = num;
                    }
                }
            }
            return "SS" + (max + 1).ToString("D2");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}