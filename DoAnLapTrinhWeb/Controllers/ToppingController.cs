using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models;

namespace DoAnLapTrinhWeb.Controllers
{
    public class ToppingController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // 1. INDEX: Lấy danh sách Topping từ CSDL
        public ActionResult Index()
        {
            var toppings = db.Toppings
                .ToList()
                .Select(t => new
                {
                    Id = t.topping_id.Trim(),
                    Name = t.name,
                    Short = t.name.Length >= 2 ? t.name.Substring(0, 2).ToUpper() : t.name.ToUpper(),
                    Price = t.price,
                    ProductCount = db.ProductToppings.Count(pt => pt.topping_id.Trim() == t.topping_id.Trim()),
                    Status = t.status == "Active" || t.status == "Đang bán" ? "Đang bán" : "Ngừng bán"
                })
                .ToList();

            ViewBag.ToppingsList = toppings;
            return View();
        }

        // 2. CREATE (POST): Thêm Topping mới
        [HttpPost]
        public ActionResult Create(string name, decimal price, string status)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "Vui lòng nhập tên topping." });
                }

                if (price < 0)
                {
                    return Json(new { success = false, message = "Giá topping không hợp lệ." });
                }

                // Tự sinh mã TP000000X tiếp theo
                string newId = "TP0000001";
                var lastItem = db.Toppings.OrderByDescending(t => t.topping_id).FirstOrDefault();
                if (lastItem != null && !string.IsNullOrEmpty(lastItem.topping_id))
                {
                    string numStr = System.Text.RegularExpressions.Regex.Match(lastItem.topping_id, @"\d+").Value;
                    if (int.TryParse(numStr, out int num))
                    {
                        newId = "TP" + (num + 1).ToString("D7");
                    }
                }

                var newTopping = new Topping
                {
                    topping_id = newId,
                    name = name.Trim(),
                    price = price,
                    status = (status == "Đang bán" || status == "Active") ? "Active" : "Inactive"
                };

                db.Toppings.Add(newTopping);
                db.SaveChanges();

                return Json(new { success = true, message = "Thêm topping thành công!" });
            }
            catch (Exception ex)
            {
                string err = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Lỗi CSDL: " + err });
            }
        }

        // 3. EDIT (POST): Cập nhật Topping
        [HttpPost]
        public ActionResult Edit(string id, string name, decimal price, string status)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return Json(new { success = false, message = "Mã topping không hợp lệ." });
                }

                string trimId = id.Trim();
                var item = db.Toppings.FirstOrDefault(t => t.topping_id.Trim() == trimId);
                if (item == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy topping cần sửa." });
                }

                item.name = name.Trim();
                item.price = price;
                item.status = (status == "Đang bán" || status == "Active") ? "Active" : "Inactive";

                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { success = true, message = "Cập nhật topping thành công!" });
            }
            catch (Exception ex)
            {
                string err = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Lỗi CSDL: " + err });
            }
        }

        // 4. DELETE (POST): Xóa Topping
        [HttpPost]
        public ActionResult Delete(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return Json(new { success = false, message = "Mã topping không hợp lệ." });
                }

                string trimId = id.Trim();
                var item = db.Toppings.FirstOrDefault(t => t.topping_id.Trim() == trimId);
                if (item != null)
                {
                    // Xóa các liên kết trong ProductToppings trước
                    var productToppings = db.ProductToppings.Where(pt => pt.topping_id.Trim() == trimId);
                    db.ProductToppings.RemoveRange(productToppings);

                    db.Toppings.Remove(item);
                    db.SaveChanges();

                    return Json(new { success = true, message = "Xóa topping thành công!" });
                }

                return Json(new { success = false, message = "Không tìm thấy topping cần xóa." });
            }
            catch (Exception ex)
            {
                string err = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Lỗi CSDL: " + err });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}