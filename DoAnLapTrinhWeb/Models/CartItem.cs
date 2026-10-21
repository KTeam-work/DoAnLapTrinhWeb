using System.Collections.Generic;
using System.Linq;

namespace DoAnLapTrinhWeb.Models
{
    // Lớp chứa thông tin Topping trong giỏ hàng
    public class CartItemTopping
    {
        public string ToppingId { get; set; }
        public string ToppingName { get; set; }
        public decimal Price { get; set; }
    }

    // Lớp chứa thông tin 1 món trong giỏ hàng
    public class CartItem
    {
        // 1. Thông tin sản phẩm cơ bản
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public decimal BasePrice { get; set; }

        // 2. Tùy chọn Size
        public string SizeId { get; set; }
        public string SizeName { get; set; }
        public decimal SizePrice { get; set; }

        // 3. Danh sách Topping khách chọn
        public List<CartItemTopping> Toppings { get; set; }

        // 4. Các thông tin thêm
        public int Quantity { get; set; }
        public string Note { get; set; }

        // 5. Thuộc tính tự động tính thành tiền của món này
        public decimal TotalPrice
        {
            get
            {
                decimal toppingsTotal = 0;
                if (Toppings != null && Toppings.Any())
                {
                    toppingsTotal = Toppings.Sum(t => t.Price);
                }
                return (BasePrice + SizePrice + toppingsTotal) * Quantity;
            }
        }
    }
}