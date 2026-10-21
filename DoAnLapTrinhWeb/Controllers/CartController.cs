using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models;

namespace DoAnLapTrinhWeb.Controllers
{
    public class CartController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();
        private CartService cartService = new CartService();

        // GET: Cart
        public ActionResult Index()
        {
            // Lấy giỏ hàng từ Session đẩy ra View
            var cart = cartService.GetCart();
            ViewBag.TotalAmount = cartService.GetTotalAmount();
            return View(cart);
        }

        // POST: Cart/Add
        // Nơi hứng dữ liệu từ Modal trang Menu
        [HttpPost]
        public ActionResult Add(string ProductId, string SizeId, List<string> ToppingIds, int Quantity, string Note)
        {
            try
            {
                
                var product = db.Products.FirstOrDefault(p => p.product_id == ProductId);
                if (product == null) return RedirectToAction("Index", "Menu");

                var size = db.ProductSizes.FirstOrDefault(s => s.size_id == SizeId);
                string sizeName = size != null ? size.size_name : "S";
                decimal sizePrice = size != null ? size.price_add : 0;

                
                var toppingList = new List<CartItemTopping>();
                if (ToppingIds != null && ToppingIds.Any())
                {
                    foreach (var tId in ToppingIds)
                    {
                        var topping = db.Toppings.FirstOrDefault(t => t.topping_id == tId);
                        if (topping != null)
                        {
                            toppingList.Add(new CartItemTopping
                            {
                                ToppingId = topping.topping_id,
                                ToppingName = topping.name,
                                Price = topping.price
                            });
                        }
                    }
                }

                //  Đóng gói thành 1 CartItem hoàn chỉnh
                var item = new CartItem
                {
                    ProductId = product.product_id,
                    ProductName = product.name,
                    ImageUrl = product.image_url,
                    BasePrice = product.base_price,
                    Quantity = Quantity > 0 ? Quantity : 1,
                    Note = Note,
                    SizeId = SizeId,
                    SizeName = sizeName,
                    SizePrice = sizePrice,
                    Toppings = toppingList
                };

                //  Gọi Service thêm vào Session
                cartService.AddItem(item);

                // Thêm xong thì chuyển hướng khách sang trang Giỏ Hàng
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                // Xử lý lỗi nếu có
                return RedirectToAction("Index", "Menu");
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