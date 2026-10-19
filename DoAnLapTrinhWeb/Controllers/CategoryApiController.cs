using DoAnLapTrinhWeb.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;


namespace DoAnLapTrinhWeb.Controllers
{
    [RoutePrefix("api/categories")]
    public class CategoryApiController : ApiController
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // 1. GET /api/categories -> Lấy toàn bộ danh mục
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetCategories()
        {
            try
            {
                var categories = db.Categories
                    .ToList()
                    .Select(c => new
                    {
                        c.category_id,
                        c.category_name,
                        product_count = db.Products.Count(p => p.category_id.Trim() == c.category_id.Trim())
                    });

                return Ok(new { success = true, data = categories });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // 2. GET /api/categories/{id} -> Chi tiết 1 danh mục
        [HttpGet]
        [Route("{id}")]
        public IHttpActionResult GetCategoryById(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Mã danh mục không hợp lệ.");

            string trimmedId = id.Trim();
            var cat = db.Categories.FirstOrDefault(c => c.category_id.Trim() == trimmedId);
            if (cat == null) return NotFound();

            return Ok(new
            {
                success = true,
                data = new
                {
                    cat.category_id,
                    cat.category_name
                }
            });
        }

        // 3. POST /api/categories -> Thêm danh mục mới
        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateCategory([FromBody] Category cat)
        {
            if (cat == null || string.IsNullOrWhiteSpace(cat.category_name))
            {
                return BadRequest("Tên danh mục không được để trống.");
            }

            try
            {
                if (string.IsNullOrEmpty(cat.category_id))
                {
                    var lastCat = db.Categories.OrderByDescending(c => c.category_id).FirstOrDefault();
                    int nextNum = 1;
                    if (lastCat != null && !string.IsNullOrEmpty(lastCat.category_id))
                    {
                        string numStr = System.Text.RegularExpressions.Regex.Match(lastCat.category_id, @"\d+").Value;
                        if (int.TryParse(numStr, out int num)) nextNum = num + 1;
                    }
                    cat.category_id = "CAT" + nextNum.ToString("D7");
                }

                db.Categories.Add(cat);
                db.SaveChanges();

                return Ok(new { success = true, message = "Thêm danh mục thành công!", category_id = cat.category_id });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // 4. PUT /api/categories/{id} -> Cập nhật danh mục
        [HttpPut]
        [Route("{id}")]
        public IHttpActionResult UpdateCategory(string id, [FromBody] Category cat)
        {
            if (string.IsNullOrEmpty(id) || cat == null || string.IsNullOrWhiteSpace(cat.category_name))
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            string trimmedId = id.Trim();
            var existingCat = db.Categories.FirstOrDefault(c => c.category_id.Trim() == trimmedId);
            if (existingCat == null) return NotFound();

            try
            {
                existingCat.category_name = cat.category_name.Trim();
                db.Entry(existingCat).State = EntityState.Modified;
                db.SaveChanges();

                return Ok(new { success = true, message = "Cập nhật danh mục thành công!" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // 5. DELETE /api/categories/{id} -> Xóa danh mục
        [HttpDelete]
        [Route("{id}")]
        public IHttpActionResult DeleteCategory(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Mã danh mục không hợp lệ.");

            string trimmedId = id.Trim();
            var cat = db.Categories.FirstOrDefault(c => c.category_id.Trim() == trimmedId);
            if (cat == null) return NotFound();

            try
            {
                // Kiểm tra xem danh mục có chứa sản phẩm không
                bool hasProducts = db.Products.Any(p => p.category_id.Trim() == trimmedId);
                if (hasProducts)
                {
                    return BadRequest("Không thể xóa danh mục đang chứa sản phẩm.");
                }

                db.Categories.Remove(cat);
                db.SaveChanges();

                return Ok(new { success = true, message = "Xóa danh mục thành công!" });
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
}