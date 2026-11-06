using System;
using System.Collections.Generic;
using System.Linq;
using DoAnLapTrinhWeb.Models;
using Newtonsoft.Json;

namespace DoAnLapTrinhWeb.Services
{
    public class TraSuaToolService
    {
        // Khởi tạo DbContext từ EDMX
        private readonly QUANLYBANTRASUAEntities _db = new QUANLYBANTRASUAEntities();

        // 1. Tìm theo gu vị
        public string SearchMenuByFlavor(string flavorQuery)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(flavorQuery)) return "Chưa nhập vị cần tìm.";
                var keywords = flavorQuery.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                var prods = _db.Products
                    .Where(p => p.status == "Available")
                    .ToList()
                    .Where(p => keywords.Any(k => (p.description != null && p.description.ToLower().Contains(k))
                                               || (p.name != null && p.name.ToLower().Contains(k))))
                    .Take(4)
                    .Select(p => new
                    {
                        TenMon = p.name,
                        MoTa = p.description,
                        GiaGoc = p.base_price
                    })
                    .ToList();

                if (!prods.Any())
                {
                    return "Không có món đúng vị này, gợi ý khách thử Trà sữa truyền thống hoặc Trà đào.";
                }

                return JsonConvert.SerializeObject(prods);
            }
            catch (Exception ex)
            {
                return "Lỗi đọc dữ liệu món: " + ex.Message;
            }
        }

        // 2. Tra cứu giá và tình trạng còn hàng
        public string CheckPriceAndStock(string itemName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(itemName)) return "Chưa nhập tên món.";
                var key = itemName.Trim().ToLower();

                var prod = _db.Products.FirstOrDefault(p => p.name.ToLower().Contains(key));
                if (prod != null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        Loai = "DoUong",
                        TenMon = prod.name,
                        Gia = prod.base_price,
                        TrangThai = prod.status == "Available" ? "Còn bán" : "Hết hàng"
                    });
                }

                var top = _db.Toppings.FirstOrDefault(t => t.name.ToLower().Contains(key));
                if (top != null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        Loai = "Topping",
                        TenTopping = top.name,
                        Gia = top.price,
                        TrangThai = top.status == "Active" ? "Còn hàng" : "Hết hàng"
                    });
                }

                return $"Không tìm thấy món hoặc topping '{itemName}' trong quán.";
            }
            catch (Exception ex)
            {
                return "Lỗi đọc giá: " + ex.Message;
            }
        }

        // 3. Tra cứu đơn hàng
        public string CheckOrderStatus(string orderId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderId)) return "Chưa nhập mã đơn.";
                var id = orderId.Trim();
                var ord = _db.Orders.FirstOrDefault(o => o.order_id == id);
                if (ord == null)
                {
                    return $"Không tìm thấy đơn hàng mã '{orderId}'.";
                }

                return JsonConvert.SerializeObject(new
                {
                    MaDon = ord.order_id,
                    TongTien = ord.total_amount,
                    TrangThai = ord.status
                });
            }
            catch (Exception ex)
            {
                return "Lỗi đọc đơn: " + ex.Message;
            }
        }
    }
}