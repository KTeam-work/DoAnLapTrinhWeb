using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DoAnLapTrinhWeb.Controllers
{
    public class OrderController : Controller
    {
        // GET: Order
        public ActionResult Checkout()
        {
            return View();
        }
        public ActionResult Details()
        {
            return View();
        }
        public ActionResult History()
        {
            return View();
        }
        public ActionResult Tracking()
        {
            return View();
        }
    }
}