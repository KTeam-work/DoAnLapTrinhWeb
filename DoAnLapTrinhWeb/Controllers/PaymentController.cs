using DoAnLapTrinhWeb.Models;
using System;
using System.Configuration;
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
        // GET: Payment/Result - Xử lý sau khi quét mã xong (Callback VietQR)
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
            ViewBag.Method = "Chuyển khoản VietQR";

            return View();
        }

        // =========================================================
        // GET: Payment/CreatePaymentUrl - Tạo link và chuyển hướng sang Cổng VNPay
        // =========================================================
        public ActionResult CreatePaymentUrl(string orderId, decimal amount)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return RedirectToAction("Index", "Home");
            }

            string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"];
            string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"];
            string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"];
            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];

            VnPayLibrary vnpay = new VnPayLibrary();

            // VNPay tính đơn vị theo VND x 100
            long vnpAmount = (long)(amount * 100);

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", vnpAmount.ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");

            // Xử lý IP khi chạy local (tránh ::1 gây lỗi bên VNPay)
            string ipAddr = Request.UserHostAddress;
            if (string.IsNullOrEmpty(ipAddr) || ipAddr == "::1")
            {
                ipAddr = "127.0.0.1";
            }
            vnpay.AddRequestData("vnp_IpAddr", ipAddr);

            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang LumiTea #" + orderId);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", orderId);

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return Redirect(paymentUrl);
        }

        // =========================================================
        // GET: Payment/VNPayReturn - Callback nhận kết quả từ VNPay trả về
        // =========================================================
        [HttpGet]
        public ActionResult VNPayReturn()
        {
            if (Request.QueryString.Count > 0)
            {
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];
                var vnpayData = Request.QueryString;
                VnPayLibrary vnpay = new VnPayLibrary();

                // Lấy tất cả tham số có tiền tố vnp_ nhưng LOẠI TRỪ vnp_SecureHash và vnp_SecureHashType
                foreach (string s in vnpayData)
                {
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        if (s != "vnp_SecureHash" && s != "vnp_SecureHashType")
                        {
                            vnpay.AddResponseData(s, vnpayData[s]);
                        }
                    }
                }

                string orderId = vnpay.GetResponseData("vnp_TxnRef")?.Trim();
                string vnpayTranId = vnpay.GetResponseData("vnp_TransactionNo");
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];

                // Kiểm tra chữ ký bảo mật
                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

                if (checkSignature)
                {
                    var payment = db.Payments.FirstOrDefault(p => p.order_id.Trim() == orderId);
                    var order = db.Orders.FirstOrDefault(o => o.order_id.Trim() == orderId);

                    // ResponseCode "00" là thành công
                    if (vnp_ResponseCode == "00")
                    {
                        if (payment != null)
                        {
                            payment.payment_status = "Paid";

                            // Cắt ngắn mã giao dịch tối đa 10 ký tự để không bị tràn cột NVARCHAR(10)
                            string rawTxn = "VNP" + vnpayTranId;
                            if (rawTxn.Length > 10)
                            {
                                rawTxn = rawTxn.Substring(0, 10);
                            }
                            payment.transaction_code = rawTxn;
                            payment.paid_at = DateTime.Now;
                        }

                        if (order != null)
                        {
                            order.status = "Confirmed";

                            // Tạo mã lịch sử HS + 8 ký tự (tổng cộng 10 ký tự vừa khít NCHAR(10))
                            string newHistoryId = "HS" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

                            // Đổi changed_by thành 'VNPay' (5 ký tự) hoặc lấy mã user để không tràn cột
                            string changeUser = !string.IsNullOrEmpty(order.user_id) ? order.user_id.Trim() : "VNPay";
                            if (changeUser.Length > 10)
                            {
                                changeUser = changeUser.Substring(0, 10);
                            }

                            db.OrderStatusHistories.Add(new OrderStatusHistory
                            {
                                history_id = newHistoryId,
                                order_id = orderId,
                                old_status = "Pending",
                                new_status = "Confirmed",
                                changed_by = changeUser,
                                changed_at = DateTime.Now
                            });
                        }

                        db.Configuration.ValidateOnSaveEnabled = false;
                        db.SaveChanges();
                        db.Configuration.ValidateOnSaveEnabled = true;

                        ViewBag.PaymentStatus = "Success";
                    }
                    else
                    {
                        if (payment != null)
                        {
                            payment.payment_status = "Failed";
                            db.SaveChanges();
                        }
                        ViewBag.PaymentStatus = "Failed";
                    }

                    decimal parsedAmount = 0;
                    if (decimal.TryParse(vnpay.GetResponseData("vnp_Amount"), out decimal rawAmount))
                    {
                        parsedAmount = rawAmount / 100;
                    }

                    ViewBag.OrderId = orderId;
                    ViewBag.TransactionCode = "VNP_" + vnpayTranId;
                    ViewBag.Amount = parsedAmount;
                    ViewBag.Method = "Cổng thanh toán VNPay";

                    return View("Result");
                }
            }

            ViewBag.PaymentStatus = "Failed";
            return View("Result");
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