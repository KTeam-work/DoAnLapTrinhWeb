using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnLapTrinhWeb.Models
{
    public class CartService
    {
        // Tên của Session key lưu giỏ hàng (Đã chốt trong file tóm tắt)
        private const string CartSessionKey = "Cart";

        // 1. Lấy Giỏ hàng hiện tại từ Session
        public List<CartItem> GetCart()
        {
            var cart = HttpContext.Current.Session[CartSessionKey] as List<CartItem>;
            if (cart == null)
            {
                cart = new List<CartItem>();
                HttpContext.Current.Session[CartSessionKey] = cart;
            }
            return cart;
        }

        // 2. Thêm món vào Giỏ
        public void AddItem(CartItem newItem)
        {
            var cart = GetCart();

            // Kiểm tra xem trong giỏ đã có món nào TRÙNG Y HỆT (cùng ID, cùng Size, cùng Topping) chưa
            var existingItem = cart.FirstOrDefault(c =>
                c.ProductId == newItem.ProductId &&
                c.SizeId == newItem.SizeId &&
                AreToppingsEqual(c.Toppings, newItem.Toppings)
            );

            if (existingItem != null)
            {
                // Nếu giống y hệt, chỉ việc tăng số lượng (Quantity)
                existingItem.Quantity += newItem.Quantity;
                // Nếu ghi chú mới khác ghi chú cũ thì nối thêm vào
                if (!string.IsNullOrEmpty(newItem.Note))
                {
                    existingItem.Note = string.IsNullOrEmpty(existingItem.Note) ? newItem.Note : existingItem.Note + "; " + newItem.Note;
                }
            }
            else
            {
                // Nếu là món mới hoàn toàn hoặc khác size/topping, thêm dòng mới
                cart.Add(newItem);
            }

            // Lưu ngược lại vào Session
            HttpContext.Current.Session[CartSessionKey] = cart;
        }

        // Hàm phụ check trùng Topping
        private bool AreToppingsEqual(List<CartItemTopping> list1, List<CartItemTopping> list2)
        {
            if (list1 == null && list2 == null) return true;
            if (list1 == null || list2 == null) return false;
            if (list1.Count != list2.Count) return false;

            var ids1 = list1.Select(t => t.ToppingId).OrderBy(id => id);
            var ids2 = list2.Select(t => t.ToppingId).OrderBy(id => id);
            return ids1.SequenceEqual(ids2);
        }

        // 3. Tính tổng tiền cả giỏ hàng
        public decimal GetTotalAmount()
        {
            var cart = GetCart();
            return cart.Sum(c => c.TotalPrice);
        }

        // 4. Xóa sạch giỏ hàng (Sau khi thanh toán xong)
        public void ClearCart()
        {
            HttpContext.Current.Session[CartSessionKey] = null;
        }
    }
}