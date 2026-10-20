using DoAnLapTrinhWeb.Models;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models;

public class CategoryController : Controller
{
    private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

    // 1. TRANG INDEX: Trả về danh sách để hiển thị
    // GET: Category/Index
    public ActionResult Index()
    {
        var categories = db.Categories.ToList();
        return View(categories);
    }

    // 2. TRANG CREATE (GET): Mở form thêm mới
    // GET: Category/Create
    public ActionResult Create()
    {
        // Tự sinh mã danh mục tiếp theo để truyền ra View nếu cần
        var lastCategory = db.Categories.OrderByDescending(c => c.category_id).FirstOrDefault();
        string nextId = "CAT0000001";
        if (lastCategory != null && !string.IsNullOrEmpty(lastCategory.category_id))
        {
            int num = int.Parse(lastCategory.category_id.Trim().Replace("CAT", "")) + 1;
            nextId = "CAT" + num.ToString("D7");
        }
        ViewBag.NextId = nextId;

        return View();
    }

    // TRANG CREATE (POST): Nhận dữ liệu từ form Create.cshtml của em gửi lên
    [HttpPost]
    public ActionResult Create(string category_id, string category_name, string description, string status)
    {
        try
        {
            if (string.IsNullOrEmpty(category_name))
            {
                return Json(new { success = false, message = "Tên danh mục không được để trống" });
            }

            Category cat = new Category()
            {
                category_id = category_id != null ? category_id.Trim() : "CAT0000001",
                category_name = category_name.Trim(),
                description = description,
                status = string.IsNullOrEmpty(status) ? "Active" : status
            };

            db.Categories.Add(cat);
            db.SaveChanges();

            // Vì code JS của em dùng window.location.href chuyển trang, ta trả về JSON hoặc Redirect
            return Json(new { success = true, redirectUrl = Url.Action("Index", "Category") });
        }
        catch (System.Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // 3. TRANG EDIT (GET): Lấy thông tin cũ đổ vào form Edit.cshtml của em
    // GET: Category/Edit?id=CAT0000001
    public ActionResult Edit(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return RedirectToAction("Index");
        }

        string trimmedId = id.Trim();
        Category category = db.Categories.FirstOrDefault(c => c.category_id.Trim() == trimmedId);

        if (category == null)
        {
            return HttpNotFound();
        }

        return View(category);
    }

    // TRANG EDIT (POST): Nhận dữ liệu cập nhật từ form Edit.cshtml của em gửi lên
    [HttpPost]
    public ActionResult Edit(string category_id, string category_name, string description, string status)
    {
        try
        {
            if (string.IsNullOrEmpty(category_id))
            {
                return Json(new { success = false, message = "Mã danh mục không hợp lệ" });
            }

            string trimmedId = category_id.Trim();
            var cat = db.Categories.FirstOrDefault(c => c.category_id.Trim() == trimmedId);

            if (cat != null)
            {
                cat.category_name = category_name != null ? category_name.Trim() : cat.category_name;
                cat.description = description;
                cat.status = status;

                db.Entry(cat).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Không tìm thấy danh mục cần sửa" });
        }
        catch (System.Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // 4. XÓA DANH MỤC: Nhận yêu cầu từ nút Xóa trong trang Edit của em
    [HttpPost]
    public ActionResult Delete(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return Json(new { success = false, message = "Mã danh mục không hợp lệ" });
        }

        string trimmedId = id.Trim();
        var category = db.Categories.FirstOrDefault(c => c.category_id.Trim() == trimmedId);

        if (category != null)
        {
            db.Categories.Remove(category);
            db.SaveChanges();
            return Json(new { success = true });
        }

        return Json(new { success = false, message = "Không tìm thấy danh mục" });
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