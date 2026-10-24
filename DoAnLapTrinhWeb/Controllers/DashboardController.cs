using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using DoAnLapTrinhWeb.Models;

namespace DoAnLapTrinhWeb.Controllers
{
    public class DashboardController : Controller
    {
        private QUANLYBANTRASUAEntities db = new QUANLYBANTRASUAEntities();

        // GET: Dashboard/Index
        public ActionResult Index()
        {
            DateTime today = DateTime.Today;
            DateTime yesterday = today.AddDays(-1);

            // 1. KPI THỐNG KÊ DOANH THU & ĐƠN HÀNG HÔM NAY
            var ordersToday = db.Orders
                .Where(o => DbFunctions.TruncateTime(o.created_at) == today)
                .ToList();

            decimal revenueToday = ordersToday
                .Where(o => o.status == "Completed")
                .Sum(o => (decimal?)o.total_amount) ?? 0;

            decimal revenueYesterday = db.Orders
                .Where(o => DbFunctions.TruncateTime(o.created_at) == yesterday && o.status == "Completed")
                .Sum(o => (decimal?)o.total_amount) ?? 0;

            double growthRate = 0;
            if (revenueYesterday > 0)
            {
                growthRate = Math.Round((double)((revenueToday - revenueYesterday) / revenueYesterday) * 100, 1);
            }

            int totalOrdersToday = ordersToday.Count;
            int preparingCount = ordersToday.Count(o => o.status == "Preparing" || o.status == "Pending");
            int completedCount = ordersToday.Count(o => o.status == "Completed");
            double completionRate = totalOrdersToday > 0 ? Math.Round((double)completedCount / totalOrdersToday * 100, 1) : 100;

            int totalCupsSold = db.OrderDetails
                .Where(od => DbFunctions.TruncateTime(od.Order.created_at) == today && od.Order.status == "Completed")
                .Sum(od => (int?)od.quantity) ?? 0;

            int onlineOrders = ordersToday.Count(o => o.source == "Online");
            int posOrders = ordersToday.Count(o => o.source == "POS");
            int onlineRate = totalOrdersToday > 0 ? (int)Math.Round((double)onlineOrders / totalOrdersToday * 100) : 50;
            int posRate = 100 - onlineRate;

            // 2. DỮ LIỆU BIỂU ĐỒ THEO GIỜ (Ép về số nguyên để không bị lỗi dấu phẩy JS)
            var hourlyData = new List<long>();
            for (int h = 8; h <= 20; h++)
            {
                decimal hourRevenue = ordersToday
                    .Where(o => o.status == "Completed" && o.created_at.Hour == h)
                    .Sum(o => (decimal?)o.total_amount) ?? 0;
                hourlyData.Add((long)hourRevenue);
            }

            // 3. CẢNH BÁO TỒN KHO NGUYÊN LIỆU
            var lowStockIngredients = db.Ingredients
                .Where(i => i.status == "Active" && i.quantity_in_stock <= i.min_threshold)
                .OrderBy(i => i.quantity_in_stock)
                .Take(5)
                .ToList();

            // 4. TOP 5 MÓN BÁN CHẠY NHẤT (Chuyển sang Tuple khớp 100% với Index.cshtml)
            var topProducts = db.OrderDetails
                .Where(od => od.Order.status == "Completed")
                .GroupBy(od => new { od.product_id, od.Product.name })
                .Select(g => new
                {
                    Name = g.Key.name,
                    Qty = g.Sum(x => x.quantity),
                    Total = g.Sum(x => x.total_price_snapshot)
                })
                .OrderByDescending(x => x.Qty)
                .Take(5)
                .AsEnumerable()
                .Select(x => Tuple.Create(x.Name, x.Qty, (decimal)x.Total))
                .ToList();

            // 5. DANH SÁCH 5 ĐƠN HÀNG GẦN NHẤT
            var recentOrders = db.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.created_at)
                .Take(5)
                .ToList();

            ViewBag.RevenueToday = revenueToday;
            ViewBag.GrowthRate = growthRate;
            ViewBag.TotalOrdersToday = totalOrdersToday;
            ViewBag.PreparingCount = preparingCount;
            ViewBag.CompletionRate = completionRate;
            ViewBag.TotalCupsSold = totalCupsSold;
            ViewBag.OnlineRate = onlineRate;
            ViewBag.PosRate = posRate;
            ViewBag.HourlyRevenueJson = string.Join(",", hourlyData);
            ViewBag.LowStockIngredients = lowStockIngredients;
            ViewBag.TopProducts = topProducts;
            ViewBag.RecentOrders = recentOrders;

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}