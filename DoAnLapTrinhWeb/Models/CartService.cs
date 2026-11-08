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


            var existingItem = cart.FirstOrDefault(c =>
                c.ProductId == newItem.ProductId &&
                c.SizeId == newItem.SizeId &&
                AreToppingsEqual(c.Toppings, newItem.Toppings) &&
                c.Note == newItem.Note
            );

            if (existingItem != null)
            {

                existingItem.Quantity += newItem.Quantity;
            }
            else
            {

                cart.Add(newItem);
            }


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

        // Cập nhật số lượng của 1 dòng trong giỏ (theo vị trí)
        public void UpdateQuantity(int index, int quantity)
        {
            var cart = GetCart();
            if (index < 0 || index >= cart.Count) return;

            if (quantity < 1) quantity = 1;
            if (quantity > 99) quantity = 99;

            cart[index].Quantity = quantity;
            HttpContext.Current.Session[CartSessionKey] = cart;
        }

        // Xóa 1 dòng khỏi giỏ (theo vị trí)
        public void RemoveItem(int index)
        {
            var cart = GetCart();
            if (index < 0 || index >= cart.Count) return;

            cart.RemoveAt(index);
            HttpContext.Current.Session[CartSessionKey] = cart;
        }

        // 4. Xóa sạch giỏ hàng (Sau khi thanh toán xong)
        public void ClearCart()
        {
            HttpContext.Current.Session[CartSessionKey] = null;
        }
    }
}