using DoAnLapTrinhWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace DoAnLapTrinhWeb.API
{
    [RoutePrefix("api/orders")]
    public class OrderApiController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // =========================================================
        // 1. GET: /api/orders
        // Lấy danh sách đơn hàng (Có thể truyền thêm ?customerId=... & status=...)
        // =========================================================
        [HttpGet]
        [Route("")]
        public JsonResult GetOrders(string customerId = "", string status = "")
        {
            try
            {
                var query = db.Orders.AsQueryable();

                if (!string.IsNullOrEmpty(customerId))
                {
                    query = query.Where(o => o.customer_id == customerId);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(o => o.status == status);
                }

                var orders = query.OrderByDescending(o => o.created_at).Select(o => new
                {
                    OrderId = o.order_id,
                    CustomerId = o.customer_id,
                    TotalAmount = o.total_amount,
                    Status = o.status,
                    DeliveryType = o.delivery_type,
                    CreatedAt = o.created_at
                }).ToList();

                return Json(new { success = true, message = "Thành công", data = orders }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message, data = (object)null }, JsonRequestBehavior.AllowGet);
            }
        }

        // =========================================================
        // 2. GET: /api/orders/{id}
        // Lấy chi tiết một đơn hàng kèm theo danh sách món và topping
        // =========================================================
        [HttpGet]
        [Route("{id}")]
        public JsonResult GetOrderDetails(string id)
        {
            try
            {
                var order = db.Orders.FirstOrDefault(o => o.order_id == id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng", data = (object)null }, JsonRequestBehavior.AllowGet);
                }

                var details = db.OrderDetails.Where(d => d.order_id == id).ToList().Select(d => new
                {
                    OrderDetailId = d.order_detail_id,
                    ProductId = d.product_id,
                    ProductName = d.Product?.name,
                    SizeId = d.size_id,
                    Quantity = d.quantity,
                    UnitPrice = d.unit_price_snapshot,
                    TotalPrice = d.total_price_snapshot,
                    Note = d.note,
                    Toppings = db.OrderDetailToppings
                                 .Where(t => t.order_detail_id == d.order_detail_id)
                                 .Select(t => new
                                 {
                                     ToppingId = t.topping_id,
                                     ToppingName = t.Topping != null ? t.Topping.name : null,
                                     Price = t.price_snapshot,
                                     Quantity = t.quantity
                                 }).ToList()
                }).ToList();

                var orderData = new
                {
                    OrderId = order.order_id,
                    CustomerId = order.customer_id,
                    TotalAmount = order.total_amount,
                    DiscountAmount = order.discount_amount,
                    Status = order.status,
                    Note = order.note,
                    CreatedAt = order.created_at,
                    Items = details
                };

                return Json(new { success = true, message = "Thành công", data = orderData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message, data = (object)null }, JsonRequestBehavior.AllowGet);
            }
        }

        // =========================================================
        // 3. POST: /api/orders
        // Dành cho App/Mobile tạo đơn hàng mới (Nhận dữ liệu dạng JSON)
        // =========================================================
        [HttpPost]
        [Route("")]
        public JsonResult CreateOrder(ApiOrderRequest request)
        {
            if (request == null || request.Items == null || !request.Items.Any())
            {
                return Json(new { success = false, message = "Dữ liệu đơn hàng không hợp lệ", data = (object)null });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    string orderId = "OD" + DateTime.Now.ToString("yyMMdd") + new Random().Next(10, 99).ToString();
                    decimal totalAmount = request.Items.Sum(i => i.TotalPrice) + (request.DeliveryType == "Delivery" ? 15000 : 0) - request.DiscountAmount;
                    if (totalAmount < 0) totalAmount = 0;

                    var newOrder = new Order
                    {
                        order_id = orderId,
                        customer_id = string.IsNullOrEmpty(request.CustomerId) ? null : request.CustomerId,
                        user_id = null,
                        source = "App", // Phân biệt đơn tạo từ App
                        delivery_type = request.DeliveryType ?? "Delivery",
                        voucher_id = string.IsNullOrEmpty(request.VoucherId) ? null : request.VoucherId,
                        discount_amount = request.DiscountAmount,
                        total_amount = totalAmount,
                        status = "Pending",
                        note = request.Note,
                        created_at = DateTime.Now
                    };
                    db.Orders.Add(newOrder);

                    foreach (var item in request.Items)
                    {
                        string orderDetailId = "DT" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

                        var orderDetail = new OrderDetail
                        {
                            order_detail_id = orderDetailId,
                            order_id = orderId,
                            product_id = item.ProductId,
                            size_id = item.SizeId,
                            quantity = item.Quantity,
                            unit_price_snapshot = item.BasePrice + item.SizePrice,
                            total_price_snapshot = item.TotalPrice,
                            note = item.Note
                        };
                        db.OrderDetails.Add(orderDetail);

                        if (item.Toppings != null && item.Toppings.Any())
                        {
                            foreach (var topping in item.Toppings)
                            {
                                var detailTopping = new OrderDetailTopping
                                {
                                    id = "TP" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                                    order_detail_id = orderDetailId,
                                    topping_id = topping.ToppingId,
                                    price_snapshot = topping.Price,
                                    quantity = item.Quantity
                                };
                                db.OrderDetailToppings.Add(detailTopping);
                            }
                        }
                    }

                    // Ghi lịch sử
                    var history = new OrderStatusHistory
                    {
                        history_id = "HS" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                        order_id = orderId,
                        old_status = null,
                        new_status = "Pending",
                        changed_at = DateTime.Now
                    };
                    db.OrderStatusHistories.Add(history);

                    db.Configuration.ValidateOnSaveEnabled = false;
                    db.SaveChanges();
                    transaction.Commit();
                    db.Configuration.ValidateOnSaveEnabled = true;

                    return Json(new { success = true, message = "Tạo đơn hàng thành công", data = new { OrderId = orderId } });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Lỗi tạo đơn: " + ex.Message, data = (object)null });
                }
            }
        }

        [HttpPut]
        [Route("{id}/status")]
        public JsonResult UpdateStatus(string id, string newStatus, string userId = null)
        {
            if (string.IsNullOrEmpty(newStatus))
            {
                return Json(new { success = false, message = "Trạng thái không được để trống", data = (object)null });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var order = db.Orders.FirstOrDefault(o => o.order_id == id);
                    if (order == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy đơn hàng", data = (object)null });
                    }

                    string oldStatus = order.status;
                    order.status = newStatus;

                    // Ghi nhận lịch sử đổi trạng thái
                    var history = new OrderStatusHistory
                    {
                        history_id = "HS" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                        order_id = order.order_id,
                        old_status = oldStatus,
                        new_status = newStatus,
                        changed_by = string.IsNullOrEmpty(userId) ? null : userId,
                        changed_at = DateTime.Now
                    };
                    db.OrderStatusHistories.Add(history);

                    // ===============================================================
                    // LOGIC TỰ ĐỘNG CHỐT THANH TOÁN & CỘNG ĐIỂM
                    // ===============================================================
                    if (newStatus == "Completed")
                    {
                        string cleanOrderId = id.Trim();

                        // 1. Chốt thanh toán (CHỈ CHO TIỀN MẶT)
                        var payment = db.Payments.FirstOrDefault(p => p.order_id.Trim() == cleanOrderId);
                        if (payment != null && payment.payment_method == "Cash" && payment.payment_status == "Pending")
                        {
                            payment.payment_status = "Paid";
                            payment.paid_at = DateTime.Now;
                        }

                        // 2. CỘNG ĐIỂM TÍCH LŨY (TẤT CẢ PHƯƠNG THỨC THANH TOÁN)
                        if (oldStatus != "Completed")
                        {
                            string cleanCusId = (order.customer_id ?? "").Trim();
                            string cleanUsrId = (order.user_id ?? "").Trim();

                            var customerData = db.Customers.FirstOrDefault(c =>
                                (!string.IsNullOrEmpty(cleanCusId) && c.customer_id.Trim() == cleanCusId) ||
                                (!string.IsNullOrEmpty(cleanUsrId) && c.user_id != null && c.user_id.Trim() == cleanUsrId));

                            if (customerData != null)
                            {
                                if (string.IsNullOrEmpty(order.customer_id))
                                {
                                    order.customer_id = customerData.customer_id;
                                }

                                // TÍNH ĐIỂM DỰA TRÊN TỔNG TIỀN ĐÃ TRỪ KHUYẾN MÃI/ĐIỂM (TotalAmount)
                                int pointsEarned = (int)(order.total_amount / 10000m);
                                customerData.loyalty_point += pointsEarned;
                            }
                        }
                    }

                    // ===============================================================
                    // HOÀN ĐIỂM KHI HỦY ĐƠN (đơn online đã dùng điểm để đổi)
                    // ===============================================================
                    if (newStatus == "Cancelled" && oldStatus != "Cancelled" && oldStatus != "Completed")
                    {
                        var m = Regex.Match(order.note ?? "", @"Đã dùng\s+(\d+)\s+điểm");
                        string cusId = (order.customer_id ?? "").Trim();
                        if (m.Success && !string.IsNullOrEmpty(cusId))
                        {
                            var cus = db.Customers.FirstOrDefault(c => c.customer_id.Trim() == cusId);
                            if (cus != null)
                            {
                                cus.loyalty_point += int.Parse(m.Groups[1].Value);
                            }
                        }
                    }

                    db.Configuration.ValidateOnSaveEnabled = false;
                    db.SaveChanges(); // Lệnh này sẽ lưu MỌI thay đổi (Order, Payment, Customer)
                    transaction.Commit();
                    db.Configuration.ValidateOnSaveEnabled = true;

                    return Json(new { success = true, message = "Cập nhật trạng thái thành công", data = new { OrderId = id, Status = newStatus } });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Lỗi cập nhật: " + ex.Message, data = (object)null });
                }
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

    // =========================================================
    // CÁC LỚP MODEL PHỤ TRỢ CHO API
    // =========================================================
    public class ApiOrderRequest
    {
        public string CustomerId { get; set; }
        public string DeliveryType { get; set; }
        public string PaymentMethod { get; set; }
        public string VoucherId { get; set; }
        public decimal DiscountAmount { get; set; }
        public string Note { get; set; }
        public List<ApiOrderDetailRequest> Items { get; set; }
    }

    public class ApiOrderDetailRequest
    {
        public string ProductId { get; set; }
        public string SizeId { get; set; }
        public int Quantity { get; set; }
        public decimal BasePrice { get; set; }
        public decimal SizePrice { get; set; }
        public string Note { get; set; }
        public List<ApiToppingRequest> Toppings { get; set; }

        public decimal TotalPrice
        {
            get
            {
                decimal toppingsTotal = Toppings != null ? Toppings.Sum(t => t.Price) : 0;
                return (BasePrice + SizePrice + toppingsTotal) * Quantity;
            }
        }
    }

    public class ApiToppingRequest
    {
        public string ToppingId { get; set; }
        public decimal Price { get; set; }
    }
}