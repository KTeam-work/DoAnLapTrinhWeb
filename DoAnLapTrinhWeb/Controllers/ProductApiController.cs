using DoAnLapTrinhWeb.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;


namespace DoAnLapTrinhWeb.Controllers
{
    [RoutePrefix("api/products")]
    public class ProductApiController : ApiController
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // 1. GET /api/products -> Danh sách sản phẩm (Filter, Phân trang, Search)
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetProducts(string search = "", string category_id = "", string status = "", int page = 1, int pageSize = 10)
        {
            try
            {
                var query = db.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductSizes)
                    .AsQueryable();

                // Lọc theo từ khóa
                if (!string.IsNullOrEmpty(search))
                {
                    string searchLower = search.Trim().ToLower();
                    query = query.Where(p => p.name.ToLower().Contains(searchLower) || p.product_id.ToLower().Contains(searchLower));
                }

                // Lọc theo danh mục
                if (!string.IsNullOrEmpty(category_id))
                {
                    query = query.Where(p => p.category_id.Trim() == category_id.Trim());
                }

                // Lọc theo trạng thái
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.status.ToLower() == status.Trim().ToLower());
                }

                int totalItems = query.Count();
                var products = query
                    .OrderBy(p => p.product_id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList()
                    .Select(p => new
                    {
                        p.product_id,
                        p.name,
                        p.description,
                        p.status,
                        category_name = p.Category != null ? p.Category.category_name : "",
                        min_price = p.ProductSizes.Any() ? p.ProductSizes.Min(s => s.price_add) : 0,
                        max_price = p.ProductSizes.Any() ? p.ProductSizes.Max(s => s.price_add) : 0
                    });

                return Ok(new
                {
                    success = true,
                    totalItems = totalItems,
                    page = page,
                    pageSize = pageSize,
                    data = products
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // 2. GET /api/products/{id} -> Chi tiết sản phẩm (Kèm Sizes, Toppings)
        [HttpGet]
        [Route("{id}")]
        public IHttpActionResult GetProductById(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Mã sản phẩm không hợp lệ.");

            string trimmedId = id.Trim();
            var product = db.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .Include(p => p.ProductToppings.Select(pt => pt.Topping))
                .FirstOrDefault(p => p.product_id.Trim() == trimmedId);

            if (product == null) return NotFound();

            var result = new
            {
                product.product_id,
                product.name,
                product.description,
                product.status,
                category_id = product.category_id,
                category_name = product.Category != null ? product.Category.category_name : "",
                sizes = product.ProductSizes.Select(s => new { s.size_id, s.size_name, s.price_add, s.status }),
                toppings = product.ProductToppings.Select(pt => new
                {
                    topping_id = pt.topping_id,
                    topping_name = pt.Topping != null ? pt.Topping.name : ""
                })
            };

            return Ok(new { success = true, data = result });
        }

        // 3. POST /api/products -> Thêm sản phẩm mới
        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateProduct([FromBody] Product prod)
        {
            if (prod == null || string.IsNullOrWhiteSpace(prod.name))
            {
                return BadRequest("Dữ liệu sản phẩm không hợp lệ.");
            }

            try
            {
                if (string.IsNullOrEmpty(prod.product_id))
                {
                    var lastProd = db.Products.OrderByDescending(p => p.product_id).FirstOrDefault();
                    int nextNum = 1;
                    if (lastProd != null && !string.IsNullOrEmpty(lastProd.product_id))
                    {
                        string numStr = System.Text.RegularExpressions.Regex.Match(lastProd.product_id, @"\d+").Value;
                        if (int.TryParse(numStr, out int num)) nextNum = num + 1;
                    }
                    prod.product_id = "SP" + nextNum.ToString("D7");
                }

                if (string.IsNullOrEmpty(prod.status)) prod.status = "Đang bán";

                db.Products.Add(prod);
                db.SaveChanges();

                return Ok(new { success = true, message = "Thêm sản phẩm thành công!", product_id = prod.product_id });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // 4. PUT /api/products/{id} -> Cập nhật sản phẩm
        [HttpPut]
        [Route("{id}")]
        public IHttpActionResult UpdateProduct(string id, [FromBody] Product prod)
        {
            if (string.IsNullOrEmpty(id) || prod == null) return BadRequest("Dữ liệu không hợp lệ.");

            string trimmedId = id.Trim();
            var existingProd = db.Products.FirstOrDefault(p => p.product_id.Trim() == trimmedId);
            if (existingProd == null) return NotFound();

            try
            {
                existingProd.name = !string.IsNullOrWhiteSpace(prod.name) ? prod.name.Trim() : existingProd.name;
                existingProd.category_id = !string.IsNullOrEmpty(prod.category_id) ? prod.category_id : existingProd.category_id;
                existingProd.description = prod.description ?? existingProd.description;
                existingProd.status = prod.status ?? existingProd.status;

                db.Entry(existingProd).State = EntityState.Modified;
                db.SaveChanges();

                return Ok(new { success = true, message = "Cập nhật sản phẩm thành công!" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // 5. DELETE /api/products/{id} -> Xóa sản phẩm
        [HttpDelete]
        [Route("{id}")]
        public IHttpActionResult DeleteProduct(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Mã sản phẩm không hợp lệ.");

            string trimmedId = id.Trim();
            var prod = db.Products.FirstOrDefault(p => p.product_id.Trim() == trimmedId);
            if (prod == null) return NotFound();

            try
            {
                // Xóa liên kết Size và Topping
                var sizes = db.ProductSizes.Where(s => s.product_id.Trim() == trimmedId);
                db.ProductSizes.RemoveRange(sizes);

                var toppings = db.ProductToppings.Where(t => t.product_id.Trim() == trimmedId);
                db.ProductToppings.RemoveRange(toppings);

                db.Products.Remove(prod);
                db.SaveChanges();

                return Ok(new { success = true, message = "Xóa sản phẩm thành công!" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

    }
    public static class ProductServiceHelper // Hoặc để chung static trong class chính nếu được
    {
        private static QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        public static Product GetById(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return null;
            string id = productId.Trim();
            return db.Products
                     .Include("ProductSizes")
                     .Include("ProductToppings")
                     .FirstOrDefault(p => p.product_id.Trim() == id);
        }

        public static List<Product> GetAvailable()
        {
            return db.Products
                     .Where(p => p.status == "Active" || p.status == "Đang bán")
                     .ToList();
        }
    }
}