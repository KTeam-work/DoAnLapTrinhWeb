using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models;

namespace DoAnLapTrinhWeb.Controllers
{
    public class MenuController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // GET: Menu
        public ActionResult Index()
        {
            
            ViewBag.Categories = db.Categories
                                   .Where(c => c.status == "Active")
                                   .ToList();

            
            var products = db.Products
                             .Where(p => p.Category.status == "Active")
                             .ToList();

            return View(products);
        }

        // GET: Menu/GetProductDetails?id={product_id}
        // Hàm này vẫn giữ nguyên để gọi AJAX cho Modal
        [HttpGet]
        public JsonResult GetProductDetails(string id)
        {
            try
            {
                var product = db.Products.FirstOrDefault(p => p.product_id == id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" }, JsonRequestBehavior.AllowGet);
                }

                var sizes = db.ProductSizes
                              .Where(s => s.product_id == id && s.status == "Active")
                              .Select(s => new {
                                  SizeId = s.size_id,
                                  SizeName = s.size_name,
                                  PriceAdd = s.price_add
                              }).ToList();

                var toppings = db.ProductToppings
                                 .Where(pt => pt.product_id == id && pt.is_allowed == true && pt.Topping.status == "Active")
                                 .Select(pt => new {
                                     ToppingId = pt.topping_id,
                                     ToppingName = pt.Topping.name,
                                     Price = pt.Topping.price
                                 }).ToList();

                return Json(new
                {
                    success = true,
                    message = "Lấy dữ liệu thành công",
                    data = new
                    {
                        ProductId = product.product_id,
                        Name = product.name,
                        BasePrice = product.base_price,
                        ImageUrl = product.image_url,
                        Sizes = sizes,
                        Toppings = toppings
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message }, JsonRequestBehavior.AllowGet);
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