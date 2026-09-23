using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DoAnLapTrinhWeb.Controllers
{
    public class CategoryController : Controller
    {
        // GET: Category/Index
        public ActionResult Index()
        {
            return View();
        }

        // GET: Category/Create
        public ActionResult Create()
        {
            return View();
        }

        // GET: Category/Edit/5
        public ActionResult Edit(int? id)
        {
            return View();
        }
    }
}