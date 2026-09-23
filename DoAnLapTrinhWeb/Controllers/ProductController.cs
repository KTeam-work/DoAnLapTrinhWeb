using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace DoAnLapTrinhWeb.Controllers
{
    public class ProductController : Controller
    {
        // =========================================================
        // DỮ LIỆU MẪU
        // Sau này sẽ thay bằng dữ liệu Database
        // =========================================================
        private static readonly List<ProductViewModel> Products =
            new List<ProductViewModel>
        {
            // =====================================================
            // TRÀ SỮA
            // =====================================================

            new ProductViewModel
            {
                ProductId = "SP0000001",
                ProductName = "Trà sữa truyền thống",
                CategoryId = "CAT0000001",
                CategoryName = "Trà sữa",
                Description = "Trà sữa truyền thống đậm vị trà, kết hợp sữa thơm béo.",
                Image = "",
                Status = "Đang bán",

                Sizes = new List<ProductSizeViewModel>
                {
                    new ProductSizeViewModel
                    {
                        SizeName = "S",
                        Price = 25000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "M",
                        Price = 30000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "L",
                        Price = 35000
                    }
                },

                Toppings = new List<string>
                {
                    "Trân châu đen",
                    "Thạch rau câu",
                    "Pudding trứng"
                }
            },


            new ProductViewModel
            {
                ProductId = "SP0000002",
                ProductName = "Trà sữa matcha",
                CategoryId = "CAT0000001",
                CategoryName = "Trà sữa",
                Description = "Trà sữa matcha thơm dịu, vị trà xanh hòa quyện cùng sữa.",
                Image = "",
                Status = "Đang bán",

                Sizes = new List<ProductSizeViewModel>
                {
                    new ProductSizeViewModel
                    {
                        SizeName = "S",
                        Price = 28000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "M",
                        Price = 33000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "L",
                        Price = 38000
                    }
                },

                Toppings = new List<string>
                {
                    "Trân châu trắng",
                    "Pudding trứng"
                }
            },


            new ProductViewModel
            {
                ProductId = "SP0000003",
                ProductName = "Trà sữa trân châu đường đen",
                CategoryId = "CAT0000001",
                CategoryName = "Trà sữa",
                Description = "Trà sữa béo thơm kết hợp trân châu đường đen ngọt đậm.",
                Image = "",
                Status = "Đang bán",

                Sizes = new List<ProductSizeViewModel>
                {
                    new ProductSizeViewModel
                    {
                        SizeName = "S",
                        Price = 30000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "M",
                        Price = 35000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "L",
                        Price = 40000
                    }
                },

                Toppings = new List<string>
                {
                    "Trân châu đen",
                    "Kem cheese"
                }
            },


            // =====================================================
            // TRÀ TRÁI CÂY
            // =====================================================

            new ProductViewModel
            {
                ProductId = "SP0000004",
                ProductName = "Trà đào cam sả",
                CategoryId = "CAT0000002",
                CategoryName = "Trà trái cây",
                Description = "Trà đào thanh mát kết hợp cùng cam và sả thơm dịu.",
                Image = "",
                Status = "Đang bán",

                Sizes = new List<ProductSizeViewModel>
                {
                    new ProductSizeViewModel
                    {
                        SizeName = "S",
                        Price = 28000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "M",
                        Price = 33000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "L",
                        Price = 38000
                    }
                },

                Toppings = new List<string>
                {
                    "Đào miếng",
                    "Thạch trái cây"
                }
            },


            new ProductViewModel
            {
                ProductId = "SP0000005",
                ProductName = "Trà vải",
                CategoryId = "CAT0000002",
                CategoryName = "Trà trái cây",
                Description = "Trà vải thanh mát, thơm nhẹ với những miếng vải ngọt dịu.",
                Image = "",
                Status = "Đang bán",

                Sizes = new List<ProductSizeViewModel>
                {
                    new ProductSizeViewModel
                    {
                        SizeName = "S",
                        Price = 27000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "M",
                        Price = 32000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "L",
                        Price = 37000
                    }
                },

                Toppings = new List<string>
                {
                    "Vải miếng",
                    "Thạch nha đam"
                }
            },


            new ProductViewModel
            {
                ProductId = "SP0000006",
                ProductName = "Trà dâu tươi",
                CategoryId = "CAT0000002",
                CategoryName = "Trà trái cây",
                Description = "Trà dâu tươi chua ngọt, thơm mát và dễ uống.",
                Image = "",
                Status = "Đang bán",

                Sizes = new List<ProductSizeViewModel>
                {
                    new ProductSizeViewModel
                    {
                        SizeName = "S",
                        Price = 30000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "M",
                        Price = 35000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "L",
                        Price = 40000
                    }
                },

                Toppings = new List<string>
                {
                    "Dâu tươi",
                    "Thạch trái cây"
                }
            },


            new ProductViewModel
            {
                ProductId = "SP0000007",
                ProductName = "Trà tắc mật ong",
                CategoryId = "CAT0000002",
                CategoryName = "Trà trái cây",
                Description = "Trà tắc kết hợp mật ong mang vị chua ngọt dễ uống.",
                Image = "",
                Status = "Đang bán",

                Sizes = new List<ProductSizeViewModel>
                {
                    new ProductSizeViewModel
                    {
                        SizeName = "S",
                        Price = 22000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "M",
                        Price = 27000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "L",
                        Price = 32000
                    }
                },

                Toppings = new List<string>
                {
                    "Thạch nha đam"
                }
            },


            // =====================================================
            // TRÀ TRUYỀN THỐNG
            // =====================================================

            new ProductViewModel
            {
                ProductId = "SP0000008",
                ProductName = "Trà chanh",
                CategoryId = "CAT0000003",
                CategoryName = "Trà truyền thống",
                Description = "Trà chanh thanh mát, vị chua nhẹ và thơm hương trà.",
                Image = "",
                Status = "Đang bán",

                Sizes = new List<ProductSizeViewModel>
                {
                    new ProductSizeViewModel
                    {
                        SizeName = "S",
                        Price = 20000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "M",
                        Price = 25000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "L",
                        Price = 30000
                    }
                },

                Toppings = new List<string>
                {
                    "Thạch nha đam"
                }
            },


            // =====================================================
            // MACCHIATO
            // =====================================================

            new ProductViewModel
            {
                ProductId = "SP0000009",
                ProductName = "Matcha Macchiato",
                CategoryId = "CAT0000004",
                CategoryName = "Macchiato",
                Description = "Matcha thơm dịu phủ lớp kem macchiato béo nhẹ.",
                Image = "",
                Status = "Đang bán",

                Sizes = new List<ProductSizeViewModel>
                {
                    new ProductSizeViewModel
                    {
                        SizeName = "S",
                        Price = 32000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "M",
                        Price = 38000
                    },
                    new ProductSizeViewModel
                    {
                        SizeName = "L",
                        Price = 43000
                    }
                },

                Toppings = new List<string>
                {
                    "Trân châu trắng",
                    "Pudding"
                }
            },


            // =====================================================
            // BÁNH
            // =====================================================

            new ProductViewModel
            {
                ProductId = "SP0000010",
                ProductName = "Bánh flan",
                CategoryId = "CAT0000006",
                CategoryName = "Bánh",
                Description = "Bánh flan mềm mịn, vị trứng sữa thơm béo.",
                Image = "",
                Status = "Đang bán",

                Sizes = new List<ProductSizeViewModel>
                {
                    new ProductSizeViewModel
                    {
                        SizeName = "S",
                        Price = 18000
                    }
                },

                Toppings = new List<string>()
            },


            new ProductViewModel
            {
                ProductId = "SP0000011",
                ProductName = "Bánh cheesecake",
                CategoryId = "CAT0000006",
                CategoryName = "Bánh",
                Description = "Bánh cheesecake mềm béo, thích hợp dùng cùng trà.",
                Image = "",
                Status = "Ngừng bán",

                Sizes = new List<ProductSizeViewModel>
                {
                    new ProductSizeViewModel
                    {
                        SizeName = "S",
                        Price = 25000
                    }
                },

                Toppings = new List<string>()
            }
        };


        // =========================================================
        // INDEX
        // Danh sách + tìm kiếm + lọc
        // =========================================================
        public ActionResult Index(
            string keyword = "",
            string category = "",
            string status = "")
        {
            IEnumerable<ProductViewModel> products = Products;


            // -----------------------------------------------------
            // TÌM KIẾM THEO MÃ HOẶC TÊN
            // -----------------------------------------------------
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                products = products.Where(x =>
                    (
                        !string.IsNullOrEmpty(x.ProductId)
                        &&
                        x.ProductId.IndexOf(
                            keyword,
                            StringComparison.OrdinalIgnoreCase) >= 0
                    )
                    ||
                    (
                        !string.IsNullOrEmpty(x.ProductName)
                        &&
                        x.ProductName.IndexOf(
                            keyword,
                            StringComparison.OrdinalIgnoreCase) >= 0
                    )
                );
            }


            // -----------------------------------------------------
            // LỌC THEO LOẠI SẢN PHẨM
            // -----------------------------------------------------
            if (!string.IsNullOrWhiteSpace(category))
            {
                products = products.Where(x =>
                    x.CategoryName == category);
            }


            // -----------------------------------------------------
            // LỌC THEO TRẠNG THÁI
            // -----------------------------------------------------
            if (!string.IsNullOrWhiteSpace(status))
            {
                products = products.Where(x =>
                    x.Status == status);
            }


            // -----------------------------------------------------
            // GỬI GIÁ TRỊ FILTER VỀ VIEW
            // -----------------------------------------------------
            ViewBag.Keyword = keyword;
            ViewBag.Category = category;
            ViewBag.Status = status;


            // -----------------------------------------------------
            // DANH SÁCH LOẠI SẢN PHẨM
            // -----------------------------------------------------
            ViewBag.Categories =
                Products
                    .Select(x => new ProductCategoryViewModel
                    {
                        CategoryId = x.CategoryId,
                        CategoryName = x.CategoryName
                    })
                    .GroupBy(x => x.CategoryName)
                    .Select(x => x.First())
                    .OrderBy(x => x.CategoryName)
                    .ToList();


            // -----------------------------------------------------
            // THỐNG KÊ
            // -----------------------------------------------------
            ViewBag.TotalProducts =
                Products.Count;

            ViewBag.ActiveProducts =
                Products.Count(x =>
                    x.Status == "Đang bán");

            ViewBag.InactiveProducts =
                Products.Count(x =>
                    x.Status == "Ngừng bán");

            ViewBag.CategoryCount =
                Products
                    .Select(x => x.CategoryName)
                    .Distinct()
                    .Count();


            return View(products.ToList());
        }


        // =========================================================
        // DETAILS
        // =========================================================
        public ActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index");
            }


            ProductViewModel product =
                Products.FirstOrDefault(x =>
                    x.ProductId == id);


            if (product == null)
            {
                return HttpNotFound();
            }


            return View(product);
        }
    }


    // =============================================================
    // PRODUCT VIEW MODEL
    // =============================================================
    public class ProductViewModel
    {
        public string ProductId { get; set; }

        public string ProductName { get; set; }

        public string CategoryId { get; set; }

        public string CategoryName { get; set; }

        public string Description { get; set; }

        public string Image { get; set; }

        public string Status { get; set; }

        public List<ProductSizeViewModel> Sizes { get; set; }

        public List<string> Toppings { get; set; }


        public ProductViewModel()
        {
            ProductId = "";
            ProductName = "";
            CategoryId = "";
            CategoryName = "";
            Description = "";
            Image = "";
            Status = "";

            Sizes = new List<ProductSizeViewModel>();

            Toppings = new List<string>();
        }
    }


    // =============================================================
    // PRODUCT SIZE
    // =============================================================
    public class ProductSizeViewModel
    {
        public string SizeName { get; set; }

        public decimal Price { get; set; }


        public ProductSizeViewModel()
        {
            SizeName = "";
            Price = 0;
        }
    }


    // =============================================================
    // PRODUCT CATEGORY
    // =============================================================
    public class ProductCategoryViewModel
    {
        public string CategoryId { get; set; }

        public string CategoryName { get; set; }


        public ProductCategoryViewModel()
        {
            CategoryId = "";
            CategoryName = "";
        }
    }
}