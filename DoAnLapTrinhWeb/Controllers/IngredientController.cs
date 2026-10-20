using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models;

namespace DoAnLapTrinhWeb.Controllers
{
    public class IngredientController : Controller
    {

        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();
        // GET: Ingredient

        public ActionResult Index(string searchString)
        {
            var inter = db.Ingredients.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower().Trim();
                inter =inter.Where(i => i.ingredient_id.ToLower().Contains(searchString)
                                          || i.name.ToLower().Contains(searchString));
            
            }
            return View(inter.ToList());
        }

        public ActionResult Create()
        {
            var lastid = db.Ingredients.OrderByDescending(t => t.ingredient_id).Select(x => x.ingredient_id).FirstOrDefault();

            int numberid = 1;
            if(lastid != null && lastid.Trim().Length > 2)
            {
                if(int.TryParse(lastid.Trim().Substring(3),out int current))
                {
                    numberid = current + 1;
                }
            }

            string nextID = $"ING{numberid:D3}";

            var model = new Ingredient
            {
                ingredient_id = nextID,
                quantity_in_stock = 0,
                min_threshold = 0,
                status = "Active"
            };

            return View(model);
        }

        // POST: Ingredient/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ingredient_id,name,unit,quantity_in_stock,min_threshold,status")] Ingredient ingredient)
        {
            if (string.IsNullOrWhiteSpace(ingredient.ingredient_id))
            {
                ModelState.AddModelError("ingredient_id", "Mã nguyên liệu không được để trống.");
            }
            else
            {
                ingredient.ingredient_id = ingredient.ingredient_id.Trim().ToUpper();
                // Kiểm tra trùng khóa chính
                if (db.Ingredients.Any(i => i.ingredient_id.Trim().ToUpper() == ingredient.ingredient_id))
                {
                    ModelState.AddModelError("ingredient_id", "Mã nguyên liệu này đã tồn tại trong hệ thống.");
                }
            }

            if (string.IsNullOrWhiteSpace(ingredient.name))
            {
                ModelState.AddModelError("name", "Tên nguyên liệu không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(ingredient.unit))
            {
                ModelState.AddModelError("unit", "Đơn vị tính không được để trống.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    ingredient.name = ingredient.name.Trim();
                    ingredient.unit = ingredient.unit.Trim();
                    if (string.IsNullOrEmpty(ingredient.status))
                    {
                        ingredient.status = "Active";
                    }

                    db.Ingredients.Add(ingredient);
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu nguyên liệu: " + ex.Message);
                }
            }

            return View(ingredient);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }


        // GET: Ingredient/Edit/IN001
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            id = id.Trim();
            var ingredient = db.Ingredients.Find(id);
            if (ingredient == null)
            {
                TempData["Error"] = "Không tìm thấy nguyên liệu yêu cầu!";
                return RedirectToAction("Index");
            }

            return View(ingredient);
        }

        // POST: Ingredient/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ingredient_id,name,unit,min_threshold,status")] Ingredient ingredient)
        {
            if (string.IsNullOrWhiteSpace(ingredient.name))
            {
                ModelState.AddModelError("name", "Tên nguyên liệu không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(ingredient.unit))
            {
                ModelState.AddModelError("unit", "Đơn vị tính không được để trống.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var itemInDb = db.Ingredients.Find(ingredient.ingredient_id.Trim());
                    if (itemInDb == null)
                    {
                        TempData["Error"] = "Nguyên liệu không tồn tại trong hệ thống!";
                        return RedirectToAction("Index");
                    }

                    // Cập nhật các thông tin được phép sửa
                    itemInDb.name = ingredient.name.Trim();
                    itemInDb.unit = ingredient.unit.Trim();
                    itemInDb.min_threshold = ingredient.min_threshold;
                    itemInDb.status = ingredient.status ?? "Active";

                 

                    db.SaveChanges();

                    TempData["Success"] = $"Đã cập nhật thông tin nguyên liệu '{itemInDb.name}' thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi cập nhật: " + ex.Message);
                }
            }

            return View(ingredient);
        }


        [HttpGet]
        public ActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

            var ingredient = db.Ingredients.Find(id.Trim());
            if (ingredient == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nguyên liệu.";
                return RedirectToAction("Index");
            }

            // 1. Kiểm tra xem nguyên liệu đã dính dữ liệu nghiệp vụ chưa
            bool isUsedInRecipe = db.Recipes.Any(r => r.ingredient_id == id);
            bool isUsedInWarehouse = db.PurchaseOrderDetails.Any(p => p.ingredient_id == id)
                                  || db.InventoryTransactions.Any(t => t.ingredient_id == id);

            if (isUsedInRecipe || isUsedInWarehouse)
            {
                // nếu đã có khóa ngoại thì xóa mềm
                ingredient.status = "Inactive";
                db.SaveChanges();

                TempData["SuccessMessage"] = $"Nguyên liệu '{ingredient.name}' đã phát sinh lịch sử kho/công thức nên hệ thống đã tự động chuyển sang trạng thái [Tạm ngừng (Inactive)] để bảo toàn dữ liệu sổ sách.";
            }
            else
            {
                
                try
                {
                    db.Ingredients.Remove(ingredient);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = $"Đã xóa hoàn toàn nguyên liệu '{ingredient.name}' khỏi hệ thống.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi xóa: " + ex.Message;
                }
            }

            return RedirectToAction("Index");
        }
    }



}