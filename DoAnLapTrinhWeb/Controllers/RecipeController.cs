using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models; // Namespace chứa các file Model Entity và DbContext

namespace DoAnLapTrinhWeb.Controllers
{
    public class RecipeController : Controller
    {
        // Khởi tạo DbContext kết nối cơ sở dữ liệu
        // Nếu tên DbContext của bạn là QuanLyTraSuaEntities hoặc tên khác, hãy đổi lại cho đúng
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // GET: Recipe/Index (Trang giao diện quản trị công thức)
        public ActionResult Index()
        {
            return View();
        }

        // 1. LẤY DANH SÁCH MÓN ĐỒ UỐNG (Cột trái)
        [HttpGet]
        public JsonResult GetDrinkList()
        {
            try
            {
                var products = db.Products
                    .Include(p => p.Category)
                    .AsEnumerable()
                    .Select(p => new
                    {
                        id = p.product_id.Trim(),
                        code = p.product_id.Trim(),
                        name = p.name,
                        category = p.Category != null ? p.Category.category_name : "Trà sữa",
                        status = p.status ?? "Active",
                        desc = p.description ?? "Chưa có mô tả công thức.",
                        base_price = p.base_price
                    }).ToList();

                return Json(new { success = true, data = products }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi tải danh sách món: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // 2. LẤY CHI TIẾT CÔNG THỨC & CÁC SIZE CỦA 1 MÓN
        [HttpGet]
        public JsonResult GetRecipeDetail(string productId)
        {
            try
            {
                if (string.IsNullOrEmpty(productId))
                {
                    return Json(new { success = false, message = "Mã sản phẩm không hợp lệ." }, JsonRequestBehavior.AllowGet);
                }

                productId = productId.Trim();
                var product = db.Products.Find(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong hệ thống." }, JsonRequestBehavior.AllowGet);
                }

                // Lấy danh sách Size theo đúng model ProductSize (price_add, size_name, size_id)
                var sizes = db.ProductSizes
                    .Where(s => s.product_id == productId && (s.status == "Active" || string.IsNullOrEmpty(s.status)))
                    .AsEnumerable()
                    .Select(s => new
                    {
                        size_id = s.size_id.Trim(),
                        size_name = s.size_name,
                        price_add = s.price_add
                    }).ToList();

                // Lấy danh sách định lượng nguyên liệu trong bảng Recipe
                var recipes = db.Recipes
                    .Include(r => r.Ingredient)
                    .Where(r => r.product_id == productId)
                    .AsEnumerable()
                    .Select(r => new
                    {
                        recipe_id = r.recipe_id.Trim(),
                        ingredient_id = r.ingredient_id.Trim(),
                        ingredient_name = r.Ingredient != null ? r.Ingredient.name : "N/A",
                        unit = !string.IsNullOrEmpty(r.unit) ? r.unit : (r.Ingredient != null ? r.Ingredient.unit : ""),
                        stock = r.Ingredient != null ? r.Ingredient.quantity_in_stock : 0,
                        quantity = r.quantity,
                        size_id = !string.IsNullOrEmpty(r.size_id) ? r.size_id.Trim() : null
                    }).ToList();

                return Json(new
                {
                    success = true,
                    product = new
                    {
                        id = product.product_id.Trim(),
                        name = product.name,
                        base_price = product.base_price,
                        desc = product.description
                    },
                    sizes = sizes,
                    recipes = recipes
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi tải chi tiết: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // 3. LẤY DANH SÁCH NGUYÊN LIỆU KHẢ DỤNG ĐỂ NẠP DROPDOWN MODAL
        [HttpGet]
        public JsonResult GetAvailableIngredients()
        {
            try
            {
                var list = db.Ingredients
                    .Where(i => i.status == "Active" || string.IsNullOrEmpty(i.status))
                    .AsEnumerable()
                    .Select(i => new
                    {
                        id = i.ingredient_id.Trim(),
                        name = i.name,
                        unit = i.unit,
                        stock = i.quantity_in_stock
                    }).ToList();

                return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi tải nguyên liệu: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // tự động tăng mã
        private string GenerateNextRecipeId()
        {
            // Lấy tất cả các mã recipe_id hiện có trong DB
            var existingIds = db.Recipes.Select(r => r.recipe_id).ToList();

            int maxNumber = 0;
            foreach (var rawId in existingIds)
            {
                if (string.IsNullOrWhiteSpace(rawId)) continue;
                string id = rawId.Trim(); // Cắt bỏ khoảng trắng do NCHAR(10)

                // Nếu mã bắt đầu bằng "RCP"
                if (id.StartsWith("RCP", StringComparison.OrdinalIgnoreCase))
                {
                    // Cắt phần số phía sau ra để parse (ví dụ "RCP003" lấy ra "003")
                    string numberPart = id.Substring(3);
                    if (int.TryParse(numberPart, out int number))
                    {
                        if (number > maxNumber)
                        {
                            maxNumber = number;
                        }
                    }
                }
            }

            return "RCP" + (maxNumber + 1).ToString("D3");
        }

        // 4.Thêm hoặc cập nhập
        [HttpPost]
        public JsonResult SaveRecipeItem(string productId, string ingredientId, decimal quantity, string unit, string sizeId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(ingredientId))
                {
                    return Json(new { success = false, message = "Thiếu thông tin sản phẩm hoặc nguyên liệu." });
                }

                if (quantity <= 0)
                {
                    return Json(new { success = false, message = "Định lượng phải lớn hơn 0." });
                }

                productId = productId.Trim();
                ingredientId = ingredientId.Trim();
                sizeId = string.IsNullOrWhiteSpace(sizeId) ? null : sizeId.Trim();

                if (string.IsNullOrWhiteSpace(unit))
                {
                    var ing = db.Ingredients.Find(ingredientId);
                    unit = ing != null ? ing.unit : "gram";
                }
                unit = unit.Trim();

                // Kiểm tra xem đã có công thức này chưa
                var existRecipe = db.Recipes.FirstOrDefault(r => r.product_id == productId
                                                              && r.ingredient_id == ingredientId
                                                              && r.size_id == sizeId);

                if (existRecipe != null)
                {
                    // Cập nhật
                    existRecipe.quantity = quantity;
                    existRecipe.unit = unit;
                }
                else
                {
                    // sinh mã
                    string nextRecipeId = GenerateNextRecipeId();

                    var newRecipe = new Recipe
                    {
                        recipe_id = nextRecipeId,
                        product_id = productId,
                        ingredient_id = ingredientId,
                        quantity = quantity,
                        unit = unit,
                        size_id = sizeId
                    };

                    db.Recipes.Add(newRecipe);
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Lưu công thức thành công!" });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => $"{x.PropertyName}: {x.ErrorMessage}");
                return Json(new { success = false, message = "Lỗi dữ liệu: " + string.Join("; ", errorMessages) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lưu: " + ex.Message });
            }
        }

        // 5. XÓA NGUYÊN LIỆU RA KHỎI CÔNG THỨC MÓN
        [HttpPost]
        public JsonResult DeleteRecipeItem(string recipeId)
        {
            try
            {
                if (string.IsNullOrEmpty(recipeId))
                {
                    return Json(new { success = false, message = "Mã công thức không hợp lệ." });
                }

                recipeId = recipeId.Trim();
                var recipe = db.Recipes.Find(recipeId);
                if (recipe != null)
                {
                    db.Recipes.Remove(recipe);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Đã xóa nguyên liệu khỏi công thức thành công." });
                }

                return Json(new { success = false, message = "Không tìm thấy dòng công thức cần xóa." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
            }
        }

        // Giải phóng vùng nhớ DbContext
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