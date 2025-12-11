using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models; // Đảm bảo có namespace này chứa ApiResponse
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace RestaurantManagementGUI.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _serializerOptions;

        // Cấu hình Base URL để nối với các endpoint tương đối (như "orders/revenue-report")
        // Bạn chỉnh port 5000 thành port thực tế của API bên bạn nhé
#if ANDROID
        private const string BaseUrl = "http://10.0.2.2:5000/api/";
#else
        private const string BaseUrl = "http://localhost:5000/api/";
#endif

        public ApiService()
        {
#if DEBUG
            var httpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true
            };
#else
            var httpHandler = new HttpClientHandler();
#endif

            _httpClient = new HttpClient(httpHandler);

            // Cấu hình JSON để không bị lỗi khi tên biến viết Hoa/thường
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        // Hàm thanh toán hóa đơn
        public async Task<bool> PayBillAsync(int maHD)
        {
            try
            {
                // URL: api/payment/pay/{maHD}
                // Dùng PostAsync vì đây là hành động thay đổi dữ liệu
                var response = await _httpClient.PostAsync($"payment/pay/{maHD}", null); // Body null vì ID đã ở trên URL

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                // Log lỗi nếu cần
                var errorContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Lỗi thanh toán: {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[API PAY LỖI] {ex.Message}");
                return false;
            }
        }
        // ==========================================
        // PHẦN MỚI THÊM: Hàm Generic dùng cho Báo cáo
        // ==========================================
        public async Task<T> GetAsync<T>(string endpoint)
        {
            try
            {
                // Tự động nối URL nếu chưa có http
                string url = endpoint.StartsWith("http") ? endpoint : $"{ApiConfig.BaseUrl}/{endpoint}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return default;

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, _serializerOptions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"API Error: {ex.Message}");
                return default;
            }
        }

        // ==========================================
        // PHẦN CODE CŨ CỦA BẠN (Giữ nguyên để không hỏng chức năng bàn)
        // ==========================================

        public async Task<List<Ban>> GetTablesAsync()
        {
            try
            {
                var url = ApiConfig.GetAllTables;
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Ban>>(json, _serializerOptions) ?? new List<Ban>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[API GET LỖI] {ex.Message}");
                return new List<Ban>();
            }
        }

        public async Task<bool> UpdateTableStatusAsync(string maBan, string newStatus)
        {
            try
            {
                var url = ApiConfig.UpdateTableStatus(maBan);
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(newStatus),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PutAsync(url, jsonContent);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[API PUT LỖI] {ex.Message}");
                return false;
            }
        }
    }
}