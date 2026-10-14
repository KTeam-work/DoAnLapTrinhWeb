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
    }
}