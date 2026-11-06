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
            string currentUserId = (Session["UserId"]?.ToString() ?? "").Trim();
            string userRole = (Session["UserRole"]?.ToString() ?? "").Trim();
            bool isAdmin = userRole == "R01" || userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

            // Tìm ca làm việc của chính nhân viên đang đăng nhập trong ngày hôm nay
            StaffShift myShiftToday = null;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                myShiftToday = db.StaffShifts
                    .Include(s => s.Shift)
                    .Include(s => s.User)
                    .FirstOrDefault(s => s.user_id.Trim() == currentUserId && DbFunctions.TruncateTime(s.work_date) == filterDate);
            }
            ViewBag.MyShiftToday = myShiftToday;
            ViewBag.SelectedDate = filterDate.ToString("yyyy-MM-dd");

            // Danh sách toàn bộ ca trực trong ngày (dành cho Admin theo dõi)
            var todayShifts = db.StaffShifts
                .Include(s => s.User)
                .Include(s => s.User.Role)
                .Include(s => s.Shift)
                .Where(s => DbFunctions.TruncateTime(s.work_date) == filterDate)
                .OrderBy(s => s.Shift.start_time)
                .ToList();

            return View(todayShifts);
        }

        // 2. BẤM "VÀO CA" (Không dùng mã PIN, tự nhận diện user đăng nhập)
        [HttpPost]
        public ActionResult CheckIn(string staffShiftId)
        {
            try
            {
                string currentUserId = (Session["UserId"]?.ToString() ?? "").Trim();
                if (string.IsNullOrEmpty(currentUserId))
                {
                    TempData["Error"] = "Vui lòng đăng nhập để thực hiện vào ca!";
                    return RedirectToAction("Login", "Account");
                }

                DateTime today = DateTime.Today;

                // Tìm ca phân công hôm nay của nhân viên
                StaffShift shiftRecord;
                if (!string.IsNullOrEmpty(staffShiftId))
                {
                    shiftRecord = db.StaffShifts.Include(s => s.Shift).FirstOrDefault(s => s.staff_shift_id.Trim() == staffShiftId.Trim());
                }
                else
                {
                    shiftRecord = db.StaffShifts
                        .Include(s => s.Shift)
                        .FirstOrDefault(s => s.user_id.Trim() == currentUserId && DbFunctions.TruncateTime(s.work_date) == today);
                }

                if (shiftRecord == null)
                {
                    TempData["Error"] = "Bạn không có lịch làm việc được phân công trong ngày hôm nay!";
                    return RedirectToAction("Index");
                }

                if (shiftRecord.check_in != null)
                {
                    TempData["Error"] = $"Bạn đã vào ca lúc {shiftRecord.check_in.Value:HH:mm:ss} rồi!";
                    return RedirectToAction("Index");
                }

                // Lưu giờ vào Database
                shiftRecord.check_in = DateTime.Now;
                db.Entry(shiftRecord).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Success"] = $"Vào ca thành công lúc {shiftRecord.check_in.Value:HH:mm:ss} (Ca: {shiftRecord.Shift?.shift_name})!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi vào ca: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // 3. BẤM "RA CA" (Lưu giờ ra & tính tổng giờ làm)
        [HttpPost]
        public JsonResult CheckOut(string staffShiftId)
        {
            try
            {
                string currentUserId = (Session["UserId"]?.ToString() ?? "").Trim();
                DateTime today = DateTime.Today;

                StaffShift shiftRecord = null;
                if (!string.IsNullOrEmpty(staffShiftId))
                {
                    shiftRecord = db.StaffShifts.Find(staffShiftId.Trim());
                }
                else if (!string.IsNullOrEmpty(currentUserId))
                {
                    shiftRecord = db.StaffShifts.FirstOrDefault(s => s.user_id.Trim() == currentUserId && DbFunctions.TruncateTime(s.work_date) == today);
                }

                if (shiftRecord == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin ca trực của bạn hôm nay!" });
                }

                if (shiftRecord.check_in == null)
                {
                    return Json(new { success = false, message = "Bạn chưa bấm 'Vào ca' nên không thể bấm 'Ra ca'!" });
                }

                if (shiftRecord.check_out != null)
                {
                    return Json(new { success = false, message = "Ca làm này đã được xác nhận kết thúc trước đó!" });
                }

                // Lưu giờ ra và tính tổng số giờ làm việc
                DateTime outTime = DateTime.Now;
                shiftRecord.check_out = outTime;

                TimeSpan duration = outTime - shiftRecord.check_in.Value;
                shiftRecord.total_hours = Math.Round((decimal)duration.TotalHours, 2);

                db.Entry(shiftRecord).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Ra ca thành công lúc {outTime:HH:mm:ss}. Tổng thời gian làm: {shiftRecord.total_hours} giờ."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi xử lý: " + ex.Message });
            }
        }

        // Các Action khác: Schedule, AssignShift, DeleteShift giữ nguyên
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
                TempData["Error"] = "Lỗi khi phân ca: " + ex.Message;
            }

            return RedirectToAction("Schedule");
        }

        [HttpPost]
        public JsonResult DeleteShift(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id)) return Json(new { success = false, message = "Mã không hợp lệ." });
                var item = db.StaffShifts.Find(id.Trim());
                if (item == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu ca trực." });

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