using DoAnLapTrinhWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DoAnLapTrinhWeb.Controllers
{
    public class OrderController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();
        private CartService cartService = new CartService();
        public class CheckoutViewModel
        {
            public List<CartItem> CartItems { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal FinalAmount { get; set; }

            // Thông tin khách hàng (Mock Data)
            public string CustomerId { get; set; }
            public string FullName { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string Address { get; set; }
        }
       
        public class OrderHistoryViewModel
        {
            public string OrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal TotalAmount { get; set; }
            public string Status { get; set; }
            public string DeliveryType { get; set; }

            // Lấy 1 hình ảnh sản phẩm đầu tiên làm ảnh đại diện cho đơn hàng
            public string FirstProductImageUrl { get; set; }
            public string FirstProductName { get; set; }
            public int OtherProductCount { get; set; }
        }

        public class OrderDetailItemViewModel
        {
            public string ProductName { get; set; }
            public string ImageUrl { get; set; }
            public string SizeName { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
            public string Note { get; set; }
            public List<string> ToppingNames { get; set; }
        }

        public class OrderDetailViewModel
        {
            public string OrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal DiscountAmount { get; set; }
            public string DeliveryType { get; set; }
            public string PaymentMethod { get; set; }
            public string PaymentStatus { get; set; }

            // Thông tin giao nhận
            public string CustomerName { get; set; }
            public string CustomerPhone { get; set; }
            // Địa chỉ không lưu trong Orders ở DB hiện tại, nên ta tạm để rỗng hoặc thêm nếu cần

            public List<OrderDetailItemViewModel> Items { get; set; }
            public List<OrderStatusHistory> StatusHistories { get; set; }
        }
        // GET: Order
        public ActionResult Checkout()
        {
            var cart = cartService.GetCart();

            // Nếu giỏ hàng trống thì đá ngược về trang Giỏ hàng
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            decimal totalAmount = cartService.GetTotalAmount();

            // Khởi tạo ViewModel và dùng dữ liệu giả lập (Mocking)
            var model = new CheckoutViewModel
            {
                CartItems = cart,
                TotalAmount = totalAmount,
                DiscountAmount = 0,
                FinalAmount = totalAmount,

                // TODO: Chờ TV1 làm xong Login sẽ đổi thành Session["UserId"]
                CustomerId = "CUS999",
                FullName = "Nguyễn Văn A",
                Phone = "0901111222",
                Email = "nguyenvana@example.com",
                Address = "123 Đường Mẫu, Phường X, Quận Y, TP.HCM"
            };

            return View(model);
        }

        // =========================================================
        // POST: Order/CheckVoucher - API gọi bằng AJAX kiểm tra mã
        // =========================================================
        [HttpPost]
        public JsonResult CheckVoucher(string code, decimal totalAmount)
        {
            try
            {
                // Tìm mã voucher đang Active
                var voucher = db.Vouchers.FirstOrDefault(v => v.code == code && v.status == "Active");

                if (voucher == null)
                {
                    return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã hết hạn!" });
                }

                // Kiểm tra thời gian sử dụng
                if (voucher.start_date > DateTime.Now || voucher.end_date < DateTime.Now)
                {
                    return Json(new { success = false, message = "Mã giảm giá không nằm trong thời gian sử dụng!" });
                }

                // Kiểm tra điều kiện đơn hàng tối thiểu
                if (totalAmount < voucher.min_order_amount)
                {
                    return Json(new { success = false, message = $"Đơn hàng tối thiểu phải từ {voucher.min_order_amount:N0}đ để áp dụng mã này!" });
                }

                // Tính tiền được giảm
                decimal discount = 0;
                if (voucher.type == "Percent") // Giảm theo %
                {
                    discount = totalAmount * (voucher.value / 100);
                }
                else if (voucher.type == "Fixed") // Giảm tiền mặt
                {
                    discount = voucher.value;
                }

                return Json(new
                {
                    success = true,
                    message = "Áp dụng mã thành công!",
                    discountAmount = discount,
                    voucherId = voucher.voucher_id
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
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

        // =========================================================
        // POST: Order/Create - Xử lý khi khách bấm "ĐẶT HÀNG"
        // =========================================================
        [HttpPost]
        public ActionResult Create(string FullName, string Phone, string Email, string Address, string OrderNote,
                                   string DeliveryType, string PaymentMethod, string VoucherId, decimal DiscountAmount = 0)
        {
            var cart = cartService.GetCart();

            // Nếu giỏ hàng trống thì không cho đặt
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // Mở một Transaction để bảo vệ dữ liệu (Có lỗi là Rollback toàn bộ)
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. TẠO MÃ ĐƠN HÀNG MỚI (Đúng 10 ký tự)
                    string orderId = "OD" + DateTime.Now.ToString("yyMMdd") + new Random().Next(10, 99).ToString();

                    // Tính tổng tiền
                    decimal totalAmount = cartService.GetTotalAmount() + (DeliveryType == "Delivery" ? 15000 : 0) - DiscountAmount;
                    if (totalAmount < 0) totalAmount = 0;

                    // Kiểm tra xem Nguoi dùng đã có trong bảng Customers chưa, nếu chưa thì để null để tránh lỗi Khóa Ngoại (FK)
                    string mockCustomerId = db.Customers.Any(c => c.customer_id == "CUS999") ? "CUS999" : null;

                    // Kiểm tra VoucherId có thật trong DB không
                    string validVoucherId = (!string.IsNullOrEmpty(VoucherId) && db.Vouchers.Any(v => v.voucher_id == VoucherId)) ? VoucherId : null;

                    // 2. LƯU VÀO BẢNG Orders
                    var newOrder = new Order
                    {
                        order_id = orderId,
                        customer_id = mockCustomerId, 
                        user_id = null,               
                        source = "Online",
                        delivery_type = string.IsNullOrEmpty(DeliveryType) ? "Delivery" : DeliveryType,
                        voucher_id = validVoucherId,
                        discount_amount = DiscountAmount,
                        total_amount = totalAmount,
                        status = "Pending",
                        note = OrderNote,
                        created_at = DateTime.Now
                    };
                    db.Orders.Add(newOrder);

                    // 3. DUYỆT GIỎ HÀNG LƯU VÀO OrderDetails VÀ OrderDetailToppings
                    foreach (var item in cart)
                    {
                        string orderDetailId = "DT" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

                        var orderDetail = new OrderDetail
                        {
                            order_detail_id = orderDetailId,
                            order_id = orderId,
                            product_id = item.ProductId != null ? item.ProductId.Trim() : "",
                            size_id = item.SizeId != null ? item.SizeId.Trim() : "",
                            quantity = item.Quantity,
                            unit_price_snapshot = item.BasePrice + item.SizePrice, 
                            total_price_snapshot = item.TotalPrice,
                            note = item.Note
                        };
                        db.OrderDetails.Add(orderDetail);

                        // Nếu có Topping thì lưu tiếp
                        if (item.Toppings != null && item.Toppings.Any())
                        {
                            foreach (var topping in item.Toppings)
                            {
                                string toppingDetailId = "TP" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

                                var detailTopping = new OrderDetailTopping
                                {
                                    id = toppingDetailId,
                                    order_detail_id = orderDetailId,
                                    topping_id = topping.ToppingId != null ? topping.ToppingId.Trim() : "",
                                    price_snapshot = topping.Price, 
                                    quantity = item.Quantity
                                };
                                db.OrderDetailToppings.Add(detailTopping);
                            }
                        }
                    }

                    // 4. LƯU LỊCH SỬ ĐƠN HÀNG VÀO OrderStatusHistory
                    var history = new OrderStatusHistory
                    {
                        history_id = "HS" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                        order_id = orderId,
                        old_status = null,
                        new_status = "Pending",
                        changed_by = null,
                        changed_at = DateTime.Now
                    };
                    db.OrderStatusHistories.Add(history);

                    // 5. CẬP NHẬT VOUCHER CỦA KHÁCH HÀNG (Nếu có xài)
                    if (validVoucherId != null && mockCustomerId != null)
                    {
                        var cusVoucher = db.CustomerVouchers.FirstOrDefault(cv => cv.customer_id == mockCustomerId && cv.voucher_id == validVoucherId && cv.status == "Unused");
                        if (cusVoucher != null)
                        {
                            cusVoucher.status = "Used";
                            cusVoucher.used_at = DateTime.Now;
                        }
                    }

                    // TODO: GỌI HÀM TRỪ KHO CỦA TV4
                    // InventoryService.DeductStockForOrder(orderId);

                    db.SaveChanges();
                    transaction.Commit();

                    // 6. XÓA SẠCH GIỎ HÀNG VÀ CHUYỂN HƯỚNG
                    cartService.ClearCart();

                    if (PaymentMethod == "Cash")
                    {
                        return RedirectToAction("Tracking", "Order", new { id = orderId });
                    }
                    else
                    {
                        return RedirectToAction("Index", "Payment", new { orderId = orderId, amount = totalAmount });
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    // Bóc tách toàn bộ InnerException để hiện rõ lỗi thật sự của SQL Server
                    string realError = ex.Message;
                    Exception inner = ex.InnerException;
                    while (inner != null)
                    {
                        realError += " --> " + inner.Message;
                        inner = inner.InnerException;
                    }

                    TempData["ErrorMessage"] = "Lỗi chi tiết: " + realError;
                    return RedirectToAction("Checkout");
                }
            }
        }


       // =========================================================
        // GET: Order/History - Xem lịch sử tất cả đơn hàng
        // =========================================================
        public ActionResult History()
        {
            string currentCustomerId = "CUS999"; // Mock data

            // Lấy danh sách các đơn hàng của khách này, sắp xếp mới nhất lên đầu
            var orders = db.Orders
                           .Where(o => o.customer_id == currentCustomerId)
                           .OrderByDescending(o => o.created_at)
                           .ToList();

            var modelList = new List<OrderHistoryViewModel>();

            foreach (var o in orders)
            {
                // Tìm sản phẩm đầu tiên của đơn hàng này để làm hình đại diện
                var firstDetail = db.OrderDetails.FirstOrDefault(d => d.order_id == o.order_id);
                string imageUrl = "~/Assets/default.jpg";
                string productName = "Sản phẩm";
                int otherCount = 0;

                if (firstDetail != null)
                {
                    imageUrl = firstDetail.Product.image_url;
                    productName = firstDetail.Product.name;
                    // Đếm xem còn bao nhiêu món khác trong đơn
                    otherCount = db.OrderDetails.Count(d => d.order_id == o.order_id) - 1;
                }

                modelList.Add(new OrderHistoryViewModel
                {
                    OrderId = o.order_id,
                    CreatedAt = o.created_at,
                    TotalAmount = o.total_amount,
                    Status = o.status,
                    DeliveryType = o.delivery_type,
                    FirstProductImageUrl = imageUrl,
                    FirstProductName = productName,
                    OtherProductCount = otherCount
                });
            }

            return View(modelList);
        }

        // =========================================================
        // GET: Order/Details - Xem chi tiết hóa đơn
        // =========================================================
        public ActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("History");

            var order = db.Orders.FirstOrDefault(o => o.order_id == id);
            if (order == null) return RedirectToAction("History");

            // Lấy thông tin thanh toán
            var payment = db.Payments.FirstOrDefault(p => p.order_id == id);

            // Lấy danh sách các món trong đơn
            var details = db.OrderDetails.Where(d => d.order_id == id).ToList();
            var itemsList = new List<OrderDetailItemViewModel>();

            foreach (var d in details)
            {
                // Lấy tên các topping của món này
                var toppings = db.OrderDetailToppings
                                 .Where(t => t.order_detail_id == d.order_detail_id)
                                 .Select(t => t.Topping.name)
                                 .ToList();
                var sizeObj = db.ProductSizes.FirstOrDefault(s => s.size_id == d.size_id);
                string resolvedSizeName = sizeObj != null ? sizeObj.size_name : d.size_id;
                itemsList.Add(new OrderDetailItemViewModel
                {
                    ProductName = d.Product.name,
                    ImageUrl = d.Product.image_url,
                    SizeName = resolvedSizeName,
                    Quantity = d.quantity,
                    UnitPrice = d.unit_price_snapshot, 
                    TotalPrice = d.total_price_snapshot,
                    Note = d.note,
                    ToppingNames = toppings
                });
            }

            // Lấy lịch sử trạng thái
            var histories = db.OrderStatusHistories
                              .Where(h => h.order_id == id)
                              .OrderByDescending(h => h.changed_at)
                              .ToList();

            var model = new OrderDetailViewModel
            {
                OrderId = order.order_id,
                CreatedAt = order.created_at,
                Status = order.status,
                TotalAmount = order.total_amount,
                DiscountAmount = order.discount_amount,
                DeliveryType = order.delivery_type,
                PaymentMethod = payment != null ? payment.payment_method : "Chưa rõ",
                PaymentStatus = payment != null ? payment.payment_status : "Chưa thanh toán",
                CustomerName = order.Customer != null ? order.Customer.full_name : "Khách vãng lai",
                CustomerPhone = order.Customer != null ? order.Customer.phone : "",
                Items = itemsList,
                StatusHistories = histories
            };

            return View(model);
        }

        // =========================================================
        // GET: Order/Tracking - Xem tiến trình giao hàng
        // =========================================================
        public ActionResult Tracking(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("History");

            var order = db.Orders.FirstOrDefault(o => o.order_id == id);
            if (order == null) return RedirectToAction("History");

            // Truyền mã đơn và trạng thái hiện tại ra View
            ViewBag.OrderId = order.order_id;
            ViewBag.Status = order.status;

            // Lấy lịch sử để vẽ thanh Timeline
            var histories = db.OrderStatusHistories
                              .Where(h => h.order_id == id)
                              .OrderByDescending(h => h.changed_at)
                              .ToList();

            return View(histories);
        }

        // =========================================================
        // POST: Order/ConfirmReceived - Khách xác nhận đã nhận hàng
        // =========================================================
        [HttpPost]
        public ActionResult ConfirmReceived(string OrderId)
        {
            var order = db.Orders.FirstOrDefault(o => o.order_id == OrderId);
            if (order != null && order.status == "Delivering")
            {
                order.status = "Completed";

                // Ghi log
                var history = new OrderStatusHistory
                {
                    history_id = "HS" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                    order_id = OrderId,
                    old_status = "Delivering",
                    new_status = "Completed",
                    changed_by = null,
                    changed_at = DateTime.Now
                };
                db.OrderStatusHistories.Add(history);

                db.Configuration.ValidateOnSaveEnabled = false;
                db.SaveChanges();
                db.Configuration.ValidateOnSaveEnabled = true;
            }

            return RedirectToAction("History");
        }
    }
}