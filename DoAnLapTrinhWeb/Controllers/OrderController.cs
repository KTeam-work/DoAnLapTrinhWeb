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

            // Thông tin khách hàng 
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
            public string Address { get; set; }
            public string Note { get; set; }
            // Thông tin giao nhận
            public string CustomerName { get; set; }
            public string CustomerPhone { get; set; }


            public List<OrderDetailItemViewModel> Items { get; set; }
            public List<OrderStatusHistory> StatusHistories { get; set; }
        }
        // =========================================================
        // CÁC LỚP MODEL HỖ TRỢ CHO POS (Nhận dữ liệu JSON từ View)
        // =========================================================
        public class PosCartItem
        {
            public string ProductId { get; set; }
            public string ProductName { get; set; }
            public string SizeId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
            public string Note { get; set; }
            public List<string> ToppingIds { get; set; }
        }

        public class PosOrderRequest
        {
            public string CustomerPhone { get; set; }
            public string CustomerName { get; set; }
            public int? PagerNumber { get; set; }
            public string PaymentMethod { get; set; }
            public decimal TotalAmount { get; set; }
            public List<PosCartItem> CartItems { get; set; }
        }
        // =========================================================
        // GET: Order/Index (Dành cho Admin và Nhân viên)
        // =========================================================
        public ActionResult Index(string status = "")
        {
            // 1. Kiểm tra trạng thái đăng nhập
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 2. Lấy quyền (Role) của người dùng từ Session
            string roleId = (Session["UserRole"]?.ToString() ?? "").Trim();

            // 3. Phân quyền: Kiểm tra nếu không phải Admin (R01) và không phải Staff (R02)
            if (roleId != "R01" && roleId != "R02")
            {
                // Khách hàng (R03) cố tình truy cập sẽ bị đẩy về trang đơn mua của họ
                return RedirectToAction("History", "Order");
            }

            // 4. Lấy danh sách đơn hàng từ CSDL
            var query = db.Orders.AsQueryable();

            // 5. Hỗ trợ lọc nhanh từ URL (Ví dụ: Order/Index?status=Pending)
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.status == status);
            }

            // 6. Sắp xếp đơn đặt mới nhất lên đầu tiên
            var orders = query.OrderByDescending(o => o.created_at).ToList();

            return View(orders);
        }
        // GET: Order/Checkout
        [HttpGet]
        public ActionResult Checkout()
        {
            // 1. Bắt buộc đăng nhập
            if (Session["UserId"] == null)
            {
                TempData["WarningMessage"] = "Vui lòng đăng nhập để tiến hành đặt hàng.";
                return RedirectToAction("Login", "Account");
            }

            // 2. Lấy giỏ hàng từ CartService 
            var cart = cartService.GetCart();
            if (cart == null || cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            // 3. Lấy thông tin user thật từ DB để gán mặc định vào form giao hàng
            string currentUserId = Session["UserId"].ToString();
            var currentUser = db.Users.FirstOrDefault(u => u.user_id == currentUserId);

            ViewBag.Cart = cart;
            ViewBag.CustomerName = currentUser?.full_name ?? Session["FullName"]?.ToString();
            ViewBag.CustomerPhone = currentUser?.phone ?? "";
            var viewModel = new CheckoutViewModel
            {
                CartItems = cart,
                TotalAmount = cartService.GetTotalAmount(),
                FullName = currentUser?.full_name ?? Session["FullName"]?.ToString(),
                Phone = currentUser?.phone ?? "",
                Email = currentUser?.email ?? "",
                Address = ""
            };
            return View(viewModel);
        }

        // POST: Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(string deliveryType, string receiverName, string receiverPhone, string deliveryAddress, string note)
        {
            // 1. Kiểm tra đăng nhập bảo mật
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = cartService.GetCart();
            if (cart == null || cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            // ========================================================
            // THÊM PHẦN VALIDATE DỮ LIỆU ĐẦU VÀO Ở ĐÂY
            // ========================================================
            if (string.IsNullOrWhiteSpace(receiverName))
            {
                ModelState.AddModelError("", "Vui lòng nhập tên người nhận.");
            }
            if (string.IsNullOrWhiteSpace(receiverPhone))
            {
                ModelState.AddModelError("", "Vui lòng nhập số điện thoại người nhận.");
            }
            if (deliveryType == "Delivery" && string.IsNullOrWhiteSpace(deliveryAddress))
            {
                ModelState.AddModelError("", "Bạn chọn Giao tận nơi, vui lòng nhập địa chỉ giao hàng.");
            }

            // Nếu có bất kỳ lỗi nào được thêm vào ModelState ở trên, trả lại View kèm thông báo lỗi
            if (!ModelState.IsValid)
            {
                ViewBag.Cart = cart;
                // Đổ lại dữ liệu khách vừa nhập để họ không phải gõ lại
                ViewBag.CustomerName = receiverName;
                ViewBag.CustomerPhone = receiverPhone;
                return View();
            }

            // 2. Lấy ID người dùng thực tế
            string currentUserId = Session["UserId"].ToString();

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 3. Khởi tạo đơn hàng mới. Prefix OD + ngày tháng năm + số ngẫu nhiên cho đủ độ dài hoặc tùy rule.
                    string newOrderId = "OD" + DateTime.Now.ToString("yyMMdd") + new Random().Next(10, 99).ToString();

                    // Map đúng các property có trong file Order.cs (source: 32)
                    Order newOrder = new Order
                    {
                        order_id = newOrderId,
                        customer_id = null, // Cập nhật logic: Nếu bảng Customer tách biệt, phải có logic lấy customer_id. Ở đây lưu user_id
                        user_id = currentUserId,
                        source = "Online",
                        delivery_type = deliveryType ?? "Delivery",
                        voucher_id = null, // Tính năng voucher tách rời
                        discount_amount = 0,
                        total_amount = cartService.GetTotalAmount(), // Gọi qua cartService 
                        status = "Pending",
                        note = note,
                        created_at = DateTime.Now
                    };


                    if (newOrder.delivery_type == "Delivery")
                    {
                        newOrder.note += string.Format(" | Giao đến: {0}, SĐT: {1}, ĐC: {2}", receiverName, receiverPhone, deliveryAddress);
                    }

                    db.Orders.Add(newOrder);

                    // 4. Lưu chi tiết đơn hàng (OrderDetails)
                    foreach (var item in cart)
                    {
                        string orderDetailId = "DT" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

                        // Map đúng các property có trong file OrderDetail.cs
                        OrderDetail detail = new OrderDetail
                        {
                            order_detail_id = orderDetailId,
                            order_id = newOrderId,
                            product_id = item.ProductId != null ? item.ProductId.Trim() : "",
                            size_id = item.SizeId != null ? item.SizeId.Trim() : "",
                            quantity = item.Quantity,
                            unit_price_snapshot = item.BasePrice + item.SizePrice,
                            total_price_snapshot = item.TotalPrice,
                            note = item.Note
                        };
                        db.OrderDetails.Add(detail);

                        // Lưu Topping nếu có
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

                    // 5. Ghi Log trạng thái đơn hàng (OrderStatusHistory)

                    OrderStatusHistory history = new OrderStatusHistory
                    {
                        history_id = "HS" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                        order_id = newOrderId,
                        old_status = null,
                        new_status = "Pending",
                        changed_by = currentUserId,
                        changed_at = DateTime.Now

                    };
                    db.OrderStatusHistories.Add(history);

                    // 6. Lưu tất cả vào Database
                    db.SaveChanges();
                    transaction.Commit();

                    // 7. Xóa giỏ hàng sau khi đặt thành công 
                    cartService.ClearCart();

                    // 8. Chuyển hướng tới trang chi tiết
                    return RedirectToAction("Details", "Order", new { id = newOrderId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Đã xảy ra lỗi: " + ex.Message);
                    ViewBag.Cart = cart;
                    return View();
                }
            }
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
            // 1. Kiểm tra đăng nhập 
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            string currentUserId = Session["UserId"].ToString();

            var cart = cartService.GetCart();

            // Nếu giỏ hàng trống thì không cho đặt
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {

                    string orderId = "OD" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();


                    // Tính tổng tiền
                    decimal totalAmount = cartService.GetTotalAmount() + (DeliveryType == "Delivery" ? 15000 : 0) - DiscountAmount;
                    if (totalAmount < 0) totalAmount = 0;

                    string validVoucherId = (!string.IsNullOrEmpty(VoucherId) && db.Vouchers.Any(v => v.voucher_id == VoucherId)) ? VoucherId : null;

                    // 2. LƯU VÀO BẢNG Orders 
                    var newOrder = new Order
                    {
                        order_id = orderId,
                        customer_id = null,
                        user_id = currentUserId,
                        source = "Online",
                        delivery_type = string.IsNullOrEmpty(DeliveryType) ? "Delivery" : DeliveryType,
                        voucher_id = validVoucherId,
                        discount_amount = DiscountAmount,
                        total_amount = totalAmount,
                        status = "Pending",
                        delivery_address = (DeliveryType == "Delivery") ? Address : "Nhận tại cửa hàng",
                        note = OrderNote,
                        created_at = DateTime.Now
                    };

                    db.Orders.Add(newOrder);

                    // 3. LƯU CHI TIẾT ĐƠN HÀNG (OrderDetails)
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

                    // 4. LƯU LỊCH SỬ ĐƠN HÀNG
                    var history = new OrderStatusHistory
                    {
                        history_id = "HS" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                        order_id = orderId,
                        old_status = null,
                        new_status = "Pending",
                        changed_by = currentUserId,
                        changed_at = DateTime.Now
                    };
                    db.OrderStatusHistories.Add(history);


                    // 5. THÊM MỚI: LƯU PHƯƠNG THỨC THANH TOÁN VÀO BẢNG Payments

                    string paymentId = "PAY" + Guid.NewGuid().ToString("N").Substring(0, 7).ToUpper();
                    string tempTransactionCode = "TXN_" + orderId + "_" + DateTime.Now.ToString("HHmmss");
                    var newPayment = new Payment
                    {
                        payment_id = paymentId,
                        order_id = orderId,
                        payment_method = string.IsNullOrEmpty(PaymentMethod) ? "Cash" : PaymentMethod,
                        amount = totalAmount,
                        payment_status = "Pending",
                        transaction_code = tempTransactionCode,
                        paid_at = null
                    };
                    db.Payments.Add(newPayment);


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
        // GET: Order/History - Xem lịch sử tất cả đơn hàng của Khách
        // =========================================================
        public ActionResult History()
        {
            // 1. Kiểm tra đăng nhập
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 2. Lấy ID người dùng thực tế
            string currentUserId = Session["UserId"].ToString();

            // 3. Lấy danh sách các đơn hàng của khách này, sắp xếp mới nhất lên đầu

            var orders = db.Orders
                           .Where(o => o.user_id == currentUserId || o.customer_id == currentUserId)
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
            // Kiểm tra đăng nhập
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(id)) return RedirectToAction("History");

            var order = db.Orders.FirstOrDefault(o => o.order_id == id);
            if (order == null) return RedirectToAction("History");

            // Lấy thông tin thanh toán
            var payment = db.Payments.FirstOrDefault(p => p.order_id == id);

            // XỬ LÝ VIỆT HÓA PHƯƠNG THỨC THANH TOÁN
            string displayPaymentMethod = "Chưa rõ";
            if (payment != null)
            {
                if (payment.payment_method == "Cash") displayPaymentMethod = "Tiền mặt";
                else if (payment.payment_method == "Banking") displayPaymentMethod = "Chuyển khoản";
                else displayPaymentMethod = payment.payment_method;
            }

            // Lấy danh sách các món trong đơn
            var details = db.OrderDetails.Where(d => d.order_id == id).ToList();
            var itemsList = new List<OrderDetailItemViewModel>();

            foreach (var d in details)
            {
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

            string finalCustomerName = order.User != null ? order.User.full_name : (order.Customer != null ? order.Customer.full_name : "Khách vãng lai");
            string finalCustomerPhone = order.User != null ? order.User.phone : (order.Customer != null ? order.Customer.phone : "");

            var model = new OrderDetailViewModel
            {
                OrderId = order.order_id,
                CreatedAt = order.created_at,
                Status = order.status,
                TotalAmount = order.total_amount,
                DiscountAmount = order.discount_amount,
                DeliveryType = order.delivery_type,
                PaymentMethod = displayPaymentMethod,
                PaymentStatus = payment != null ? payment.payment_status : "Chưa thanh toán",
                CustomerName = finalCustomerName,
                CustomerPhone = finalCustomerPhone,
                Items = itemsList,
                StatusHistories = histories,
                Address = order.delivery_address,
                Note = order.note,
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
            ViewBag.DeliveryType = order.delivery_type;
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

                // THÊM MỚI: TỰ ĐỘNG CHUYỂN TRẠNG THÁI THANH TOÁN NẾU LÀ TIỀN MẶT
                var payment = db.Payments.FirstOrDefault(p => p.order_id == OrderId);
                if (payment != null && payment.payment_method == "Cash" && payment.payment_status == "Pending")
                {
                    payment.payment_status = "Paid";
                    payment.paid_at = DateTime.Now;
                }

                // Ghi log
                var history = new OrderStatusHistory
                {
                    history_id = "HS" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                    order_id = OrderId,
                    old_status = "Delivering",
                    new_status = "Completed",
                    changed_by = Session["UserId"]?.ToString(),
                    changed_at = DateTime.Now
                };
                db.OrderStatusHistories.Add(history);

                db.Configuration.ValidateOnSaveEnabled = false;
                db.SaveChanges();
                db.Configuration.ValidateOnSaveEnabled = true;
            }

            return RedirectToAction("History");
        }
        // =========================================================
        // GET: Order/POS - Màn hình Bán hàng tại quầy
        // =========================================================
        [HttpGet]
        public ActionResult POS()
        {
            // 1. Chỉ cho phép Nhân viên (R02) và Admin (R01) truy cập
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            string roleId = (Session["UserRole"]?.ToString() ?? "").Trim();
            if (roleId != "R01" && roleId != "R02") return RedirectToAction("History", "Order");

            // 2. Load danh sách danh mục, sản phẩm, toàn bộ size theo từng món, và topping
            ViewBag.Categories = db.Categories.Where(c => c.status == "Active").ToList();
            ViewBag.Products = db.Products.Where(p => p.status == "Available").ToList();

            // Lấy toàn bộ Size đang Active (KHÔNG dùng GroupBy nữa vì mỗi món có mã size_id và giá cộng thêm riêng)
            ViewBag.Sizes = db.ProductSizes.Where(s => s.status == "Active").ToList();

            // Sửa 'Available' thành 'Active' cho khớp với CHECK CONSTRAINT của bảng Toppings trong QLTS.sql
            ViewBag.Toppings = db.Toppings.Where(t => t.status == "Active").ToList();

            return View();
        }

        // =========================================================
        // POST: Order/CreatePosOrder - API Xử lý tính tiền bằng AJAX
        // =========================================================
        [HttpPost]
        public JsonResult CreatePosOrder(PosOrderRequest request)
        {
            if (Session["UserId"] == null) return Json(new { success = false, message = "Phiên làm việc hết hạn!" });
            string currentUserId = Session["UserId"].ToString();

            if (request.CartItems == null || !request.CartItems.Any())
                return Json(new { success = false, message = "Giỏ hàng đang trống!" });

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. KIỂM TRA RÀNG BUỘC THẺ RUNG
                    // 1. KIỂM TRA RÀNG BUỘC THẺ RUNG (Dùng trực tiếp cột pager_number)
                    if (request.PagerNumber.HasValue)
                    {
                        bool isPagerBusy = db.Orders.Any(o => o.pager_number == request.PagerNumber.Value &&
                                          (o.status == "Pending" || o.status == "Confirmed" || o.status == "Preparing" || o.status == "Ready"));

                        if (isPagerBusy) return Json(new { success = false, message = $"Thẻ rung số {request.PagerNumber.Value} đang bận!" });
                    }

                    // 2. XỬ LÝ KHÁCH HÀNG
                    string customerId = null;
                    string userIdForOrder = null;
                    string optAlert = "";

                    if (!string.IsNullOrEmpty(request.CustomerPhone))
                    {
                        var existingCustomer = db.Customers.FirstOrDefault(c => c.phone == request.CustomerPhone);
                        if (existingCustomer != null)
                        {
                            customerId = existingCustomer.customer_id;
                            userIdForOrder = existingCustomer.user_id;
                            // Cộng điểm
                            existingCustomer.loyalty_point += (int)(request.TotalAmount / 10000);
                        }
                        else
                        {
                            // NẾU KHÁCH HÀNG CHƯA ĐĂNG KÝ, BÁO LỖI VÀ CHẶN LẠI
                            return Json(new { success = false, message = "Số điện thoại chưa đăng ký thành viên! Vui lòng xóa SĐT hoặc đăng ký tài khoản trên Website." });
                        }
                    }

                    // 3. TẠO ĐƠN HÀNG (TRẠNG THÁI TỰ ĐỘNG)
                    string orderId = "OD" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                    string finalStatus = request.PagerNumber.HasValue ? "Preparing" : "Completed";

                    var newOrder = new Order
                    {
                        order_id = orderId,
                        customer_id = customerId,
                        user_id = userIdForOrder,
                        source = "POS",
                        delivery_type = "Pickup",
                        total_amount = request.TotalAmount,
                        status = finalStatus,
                        delivery_address = "Tại quầy",
                        note = "", 
                        pager_number = request.PagerNumber, 
                        created_at = DateTime.Now
                    };
                    db.Orders.Add(newOrder);

                    // 4. LƯU CHI TIẾT
                    foreach (var item in request.CartItems)
                    {
                        string orderDetailId = "DT" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                        string cleanProdId = (item.ProductId ?? "").Trim();
                        string cleanSizeId = (item.SizeId ?? "").Trim();

                        // Kiểm tra chính xác cặp (product_id, size_id) trong bảng ProductSizes
                        var matchedSize = db.ProductSizes.FirstOrDefault(s =>
                            s.product_id.Trim() == cleanProdId &&
                            s.size_id.Trim() == cleanSizeId);

                        // Nếu vì lý do gì đó không khớp, tự động lấy Size đầu tiên của đúng món đó
                        if (matchedSize == null)
                        {
                            matchedSize = db.ProductSizes.FirstOrDefault(s => s.product_id.Trim() == cleanProdId);
                        }

                        if (matchedSize == null)
                        {
                            throw new Exception($"Món '{item.ProductName}' chưa được cấu hình Size trong bảng ProductSizes!");
                        }

                        var orderDetail = new OrderDetail
                        {
                            order_detail_id = orderDetailId,
                            order_id = orderId,
                            product_id = matchedSize.product_id, // Lấy chuẩn NCHAR(10) từ DB
                            size_id = matchedSize.size_id,       // Lấy chuẩn NCHAR(10) từ DB, khớp 100% FK_OrderDetails_ProductSize
                            quantity = item.Quantity,
                            unit_price_snapshot = item.UnitPrice,
                            total_price_snapshot = item.TotalPrice,
                            note = item.Note
                        };
                        db.OrderDetails.Add(orderDetail);

                        // LƯU TOPPING
                        if (item.ToppingIds != null && item.ToppingIds.Any())
                        {
                            foreach (var topId in item.ToppingIds)
                            {
                                string cleanTopId = (topId ?? "").Trim();
                                var toppingInfo = db.Toppings.FirstOrDefault(t => t.topping_id.Trim() == cleanTopId);
                                if (toppingInfo != null)
                                {
                                    var detailTopping = new OrderDetailTopping
                                    {
                                        id = "TP" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                                        order_detail_id = orderDetailId,
                                        topping_id = toppingInfo.topping_id,
                                        price_snapshot = toppingInfo.price,
                                        quantity = item.Quantity
                                    };
                                    db.OrderDetailToppings.Add(detailTopping);
                                }
                            }
                        }
                    }

                    // 5. GHI LOG TRẠNG THÁI
                    db.OrderStatusHistories.Add(new OrderStatusHistory
                    {
                        history_id = "HS" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                        order_id = orderId,
                        new_status = finalStatus,
                        changed_by = currentUserId,
                        changed_at = DateTime.Now
                    });

                    // 6. THANH TOÁN 
                    db.Payments.Add(new Payment
                    {
                        payment_id = "PAY" + Guid.NewGuid().ToString("N").Substring(0, 7).ToUpper(),
                        order_id = orderId,
                        payment_method = string.IsNullOrEmpty(request.PaymentMethod) ? "Cash" : request.PaymentMethod,
                        amount = request.TotalAmount,
                        payment_status = "Paid",
                        transaction_code = "POS_" + orderId,
                        paid_at = DateTime.Now
                    });

                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, orderId = orderId, message = "Thanh toán xong! " + optAlert });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    string realError = ex.Message;
                    Exception inner = ex.InnerException;
                    while (inner != null)
                    {
                        realError += " --> " + inner.Message;
                        inner = inner.InnerException;
                    }

                    return Json(new { success = false, message = "Lỗi: " + realError });
                }
            }
        }
    }
}