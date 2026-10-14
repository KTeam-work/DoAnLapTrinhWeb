using DoAnLapTrinhWeb.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace DoAnLapTrinhWeb.API
{
    // Sử dụng RoutePrefix để tự động gắn chữ /api/vouchers cho tất cả các hàm bên trong
    [RoutePrefix("api/vouchers")]
    public class VoucherApiController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // =========================================================
        // GET: /api/vouchers
        // Lấy danh sách tất cả voucher đang khả dụng (Active và còn hạn)
        // =========================================================
        [HttpGet]
        [Route("")]
        public JsonResult GetAvailableVouchers()
        {
            try
            {
                var vouchers = db.Vouchers
                    .Where(v => v.status == "Active" && v.end_date >= DateTime.Now)
                    .Select(v => new {
                        VoucherId = v.voucher_id,
                        Code = v.code,
                        Type = v.type,
                        Value = v.value,
                        MinOrderAmount = v.min_order_amount,
                        StartDate = v.start_date,
                        EndDate = v.end_date
                    }).ToList();

                // Trả về đúng chuẩn { success, message, data }
                return Json(new
                {
                    success = true,
                    message = "Lấy danh sách Voucher thành công",
                    data = vouchers
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message, data = (object)null }, JsonRequestBehavior.AllowGet);
            }
        }

        // =========================================================
        // GET: /api/vouchers/check?code={mã}&amount={tổng_tiền}
        // Kiểm tra mã giảm giá có hợp lệ với đơn hàng không
        // =========================================================
        [HttpGet]
        [Route("check")]
        public JsonResult CheckVoucher(string code, decimal amount)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Json(new { success = false, message = "Vui lòng cung cấp mã giảm giá", data = (object)null }, JsonRequestBehavior.AllowGet);
                }

                var voucher = db.Vouchers.FirstOrDefault(v => v.code == code && v.status == "Active");

                if (voucher == null)
                {
                    return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã bị khóa!", data = (object)null }, JsonRequestBehavior.AllowGet);
                }

                if (voucher.start_date > DateTime.Now || voucher.end_date < DateTime.Now)
                {
                    return Json(new { success = false, message = "Mã giảm giá không nằm trong thời gian sử dụng!", data = (object)null }, JsonRequestBehavior.AllowGet);
                }

                if (amount < voucher.min_order_amount)
                {
                    return Json(new { success = false, message = $"Đơn hàng tối thiểu phải từ {voucher.min_order_amount:N0}đ để áp dụng mã này!", data = (object)null }, JsonRequestBehavior.AllowGet);
                }

                // Tính toán số tiền được giảm
                decimal discount = 0;
                if (voucher.type == "Percent")
                {
                    discount = amount * (voucher.value / 100);
                }
                else if (voucher.type == "Fixed")
                {
                    discount = voucher.value;
                }

                return Json(new
                {
                    success = true,
                    message = "Áp dụng mã thành công!",
                    data = new
                    {
                        DiscountAmount = discount,
                        VoucherId = voucher.voucher_id
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message, data = (object)null }, JsonRequestBehavior.AllowGet);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}