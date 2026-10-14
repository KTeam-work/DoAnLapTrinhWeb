using DoAnLapTrinhWeb.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace DoAnLapTrinhWeb.Controllers
{
    public class PaymentController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // =========================================================
        // GET: Payment/Index - Hiển thị mã QR và tạo giao dịch Pending
        // =========================================================
        public ActionResult Index(string orderId, decimal amount = 0)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra xem đơn hàng này đã có record Payment chưa, nếu chưa thì tạo mới
            var existingPayment = db.Payments.FirstOrDefault(p => p.order_id == orderId);
            if (existingPayment == null)
            {
                var payment = new Payment
                {
                    // Tạo mã NCHAR(10) an toàn
                    payment_id = "PM" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                    order_id = orderId,
                    payment_method = "Banking",
                    amount = amount,
                    payment_status = "Pending", // Đang chờ quét mã
                    transaction_code = null,
                    paid_at = null
                };

                db.Configuration.ValidateOnSaveEnabled = false;
                db.Payments.Add(payment);
                db.SaveChanges();
                db.Configuration.ValidateOnSaveEnabled = true;
            }

            // Truyền dữ liệu ra View để hiển thị mã QR và số tiền
            ViewBag.OrderId = orderId;
            ViewBag.Amount = amount;

            return View();
        }

        // =========================================================
        // GET: Payment/Result - Xử lý sau khi quét mã xong (Callback)
        // =========================================================
        public ActionResult Result(string orderId, string status)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return RedirectToAction("Index", "Home");
            }

            // Tìm record Payment của đơn hàng này
            var payment = db.Payments.FirstOrDefault(p => p.order_id == orderId);
            var order = db.Orders.FirstOrDefault(o => o.order_id == orderId);

            if (payment != null && order != null)
            {
                if (status == "success")
                {
                    // 1. Cập nhật bảng Payments
                    payment.payment_status = "Paid";
                    payment.transaction_code = "VNQ-" + new Random().Next(100000, 999999).ToString();
                    payment.paid_at = DateTime.Now;

                    // 2. Cập nhật bảng Orders sang "Confirmed" (Đã xác nhận)
                    order.status = "Confirmed";

                    // 3. Ghi lịch sử trạng thái mới vào OrderStatusHistory
                    var history = new OrderStatusHistory
                    {
                        history_id = "HS" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                        order_id = orderId,
                        old_status = "Pending",
                        new_status = "Confirmed",
                        changed_by = null,
                        changed_at = DateTime.Now
                    };
                    db.OrderStatusHistories.Add(history);
                }
                else
                {
                    // Nếu thất bại (khách hủy thanh toán)
                    payment.payment_status = "Failed";
                }

                db.Configuration.ValidateOnSaveEnabled = false;
                db.SaveChanges();
                db.Configuration.ValidateOnSaveEnabled = true;
            }

            // Truyền trạng thái và dữ liệu ra View để hiển thị thông báo
            ViewBag.PaymentStatus = status == "success" ? "Success" : "Failed";
            ViewBag.OrderId = orderId;
            ViewBag.TransactionCode = payment?.transaction_code;
            ViewBag.Amount = payment?.amount ?? 0;

            return View();
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