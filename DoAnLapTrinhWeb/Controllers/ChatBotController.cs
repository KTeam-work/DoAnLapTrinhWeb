
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Configuration;
using DoAnLapTrinhWeb.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DoAnLapTrinhWeb.Controllers
{
    public class ChatBotController : Controller
    {
        private readonly TraSuaToolService _toolService =
            new TraSuaToolService();

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Ask(string userMessage)
        {
            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.SecurityProtocolType.Tls12;

            // Lấy API Key từ Web.config
            string apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Json(new
                {
                    reply = "Chưa cấu hình Gemini API Key."
                });
            }

            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return Json(new
                {
                    reply = "Bạn vui lòng nhập câu hỏi nhé!"
                });
            }

            string url =
                "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent?key="
                + apiKey;

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(60);

                    // =========================================
                    // 1. KHAI BÁO CÁC TOOL
                    // =========================================

                    var tools = new object[]
                    {
                        new
                        {
                            function_declarations = new object[]
                            {
                                new
                                {
                                    name = "SearchMenuByFlavor",
                                    description = "Tìm món theo gu vị (ngọt, béo, chua, thanh mát)",
                                    parameters = new
                                    {
                                        type = "OBJECT",
                                        properties = new
                                        {
                                            flavorQuery = new
                                            {
                                                type = "STRING",
                                                description = "Vị khách muốn uống"
                                            }
                                        },
                                        required = new[] { "flavorQuery" }
                                    }
                                },

                                new
                                {
                                    name = "CheckPriceAndStock",
                                    description = "Tra cứu giá và tình trạng còn/hết của món hoặc topping",
                                    parameters = new
                                    {
                                        type = "OBJECT",
                                        properties = new
                                        {
                                            itemName = new
                                            {
                                                type = "STRING",
                                                description = "Tên món hoặc topping"
                                            }
                                        },
                                        required = new[] { "itemName" }
                                    }
                                },

                                new
                                {
                                    name = "CheckOrderStatus",
                                    description = "Kiểm tra tình trạng đơn hàng bằng mã đơn",
                                    parameters = new
                                    {
                                        type = "OBJECT",
                                        properties = new
                                        {
                                            orderId = new
                                            {
                                                type = "STRING",
                                                description = "Mã đơn hàng"
                                            }
                                        },
                                        required = new[] { "orderId" }
                                    }
                                }
                            }
                        }
                    };

                    // =========================================
                    // 2. GỬI CÂU HỎI LƯỢT 1 CHO GEMINI
                    // =========================================

                    string prompt =
                         "Bạn là trợ lý trà sữa thân thiện của cửa hàng LumiTea. " +
                         "Hãy trả lời bằng tiếng Việt, ngắn gọn, dễ hiểu và thân thiện. " +

                         "Khi khách hỏi về gu vị, hãy sử dụng SearchMenuByFlavor. " +
                         "Khi khách hỏi giá hoặc tình trạng món, hãy sử dụng CheckPriceAndStock. " +
                         "Khi khách hỏi đơn hàng, hãy sử dụng CheckOrderStatus. " +

                         "QUY TRÌNH ĐẶT HÀNG TẠI LUMITEA: " +
                         "Khi khách hỏi cách đặt hàng hoặc muốn mua trà sữa, hãy hướng dẫn khách truy cập mục Thực đơn trên website. " +
                         "Đường dẫn trang Thực đơn là /Menu/Index. " +
                         "Khách chọn món uống muốn mua, chọn size, topping và số lượng nếu có, sau đó thêm món vào giỏ hàng. " +
                         "Tiếp theo, khách mở giỏ hàng, kiểm tra đơn hàng và tiến hành đặt hàng. " +
                         "Luôn hướng dẫn khách nhấn vào mục Thực đơn trên thanh menu để bắt đầu đặt hàng. " +
                         "Không tự nhận đã đặt hàng thành công khi khách chưa hoàn tất thao tác. " +

                         "Khi hướng dẫn đặt hàng, hãy cung cấp đường dẫn /Menu/Index để khách truy cập. " +
                         "Không tự bịa dữ liệu về sản phẩm, giá cả hoặc đơn hàng. " +

                         "Câu hỏi của khách: " + userMessage;

                    var payload = new
                    {
                        contents = new object[]
                        {
                            new
                            {
                                role = "user",
                                parts = new object[]
                                {
                                    new { text = prompt }
                                }
                            }
                        },
                        tools = tools
                    };

                    var content = new StringContent(
                        JsonConvert.SerializeObject(payload),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var response = await client.PostAsync(url, content);

                    var jsonStr =
                        await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        return Json(new
                        {
                            reply = "Google trả về mã " +
                                    (int)response.StatusCode + ": " +
                                    jsonStr
                        });
                    }

                    var geminiJson = JObject.Parse(jsonStr);

                    var modelContent =
                        geminiJson["candidates"]?[0]?["content"];

                    if (modelContent == null)
                    {
                        return Json(new
                        {
                            reply = "Không nhận được phản hồi từ Gemini."
                        });
                    }

                    var parts = modelContent["parts"] as JArray;

                    if (parts == null || !parts.Any())
                    {
                        return Json(new
                        {
                            reply = "Gemini không trả về nội dung."
                        });
                    }

                    // =========================================
                    // 3. KIỂM TRA GEMINI CÓ GỌI TOOL KHÔNG
                    // =========================================

                    var callPart = parts.FirstOrDefault(
                        p => p["functionCall"] != null
                    );

                    // Nếu không gọi Tool thì trả lời bình thường
                    if (callPart == null)
                    {
                        string directReply = string.Join(
                            "\n",
                            parts
                                .Where(p => p["text"] != null)
                                .Select(p => p["text"].ToString())
                        );

                        return Json(new
                        {
                            reply = string.IsNullOrWhiteSpace(directReply)
                                ? "Xin chào, em có thể giúp gì cho bạn?"
                                : directReply
                        });
                    }

                    // =========================================
                    // 4. XỬ LÝ TOOL
                    // =========================================

                    var functionCall = callPart["functionCall"];

                    string funcName =
                        functionCall["name"]?.ToString();

                    var args = functionCall["args"];

                    string toolResult = "";

                    if (funcName == "SearchMenuByFlavor")
                    {
                        string flavor =
                            args?["flavorQuery"]?.ToString() ?? "";

                        toolResult =
                            _toolService.SearchMenuByFlavor(flavor);
                    }
                    else if (funcName == "CheckPriceAndStock")
                    {
                        string itemName =
                            args?["itemName"]?.ToString() ?? "";

                        toolResult =
                            _toolService.CheckPriceAndStock(itemName);
                    }
                    else if (funcName == "CheckOrderStatus")
                    {
                        string orderId =
                            args?["orderId"]?.ToString() ?? "";

                        toolResult =
                            _toolService.CheckOrderStatus(orderId);
                    }
                    else
                    {
                        return Json(new
                        {
                            reply = "Không tìm thấy chức năng: " + funcName
                        });
                    }

                    // =========================================
                    // 5. TẠO FUNCTION RESPONSE
                    // =========================================

                    var functionResponse = new JObject
                    {
                        ["name"] = funcName,
                        ["response"] = new JObject
                        {
                            ["output"] = toolResult
                        }
                    };

                    // Giữ ID nếu Gemini có trả về
                    string callId =
                        functionCall["id"]?.ToString();

                    if (!string.IsNullOrEmpty(callId))
                    {
                        functionResponse["id"] = callId;
                    }

                    // =========================================
                    // 6. TẠO PAYLOAD LƯỢT 2
                    // =========================================

                    // QUAN TRỌNG:
                    // Giữ nguyên toàn bộ modelContent.
                    // Không tạo lại functionCall thủ công.
                    // Việc này bảo toàn thought_signature.

                    var followUpPayload = new JObject
                    {
                        ["contents"] = new JArray
                        {
                            // Tin nhắn ban đầu của khách
                            new JObject
                            {
                                ["role"] = "user",
                                ["parts"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["text"] = prompt
                                    }
                                }
                            },

                            // Toàn bộ phản hồi Gemini lượt 1
                            // Bao gồm thought_signature
                            modelContent.DeepClone(),

                            // Kết quả trả về từ Tool
                            new JObject
                            {
                                ["role"] = "user",
                                ["parts"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["functionResponse"] =
                                            functionResponse
                                    }
                                }
                            }
                        }
                    };

                    // =========================================
                    // 7. GỬI KẾT QUẢ TOOL LẠI CHO GEMINI
                    // =========================================

                    var followUpContent = new StringContent(
                        followUpPayload.ToString(Formatting.None),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var followUpRes =
                        await client.PostAsync(url, followUpContent);

                    var followUpJsonStr =
                        await followUpRes.Content.ReadAsStringAsync();

                    if (!followUpRes.IsSuccessStatusCode)
                    {
                        return Json(new
                        {
                            reply = "Lỗi lượt 2 từ Google: " +
                                    followUpJsonStr
                        });
                    }

                    // =========================================
                    // 8. NHẬN CÂU TRẢ LỜI CUỐI CÙNG
                    // =========================================

                    var finalJson =
                        JObject.Parse(followUpJsonStr);

                    var finalParts =
                        finalJson["candidates"]?[0]?["content"]?["parts"]
                        as JArray;

                    if (finalParts == null)
                    {
                        return Json(new
                        {
                            reply = "Không nhận được kết quả cuối từ Gemini."
                        });
                    }

                    string finalReply = string.Join(
                        "\n",
                        finalParts
                            .Where(p => p["text"] != null)
                            .Select(p => p["text"].ToString())
                    );

                    return Json(new
                    {
                        reply = string.IsNullOrWhiteSpace(finalReply)
                            ? "Không tạo được phản hồi sau khi tra cứu."
                            : finalReply
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    reply = "Lỗi Exception: " + ex.Message
                });
            }
        }
    }
}
