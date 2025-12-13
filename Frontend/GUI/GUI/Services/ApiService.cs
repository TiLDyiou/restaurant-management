using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace RestaurantManagementGUI.Services
{
    public class ApiService 
    {
        // HttpClient này được tiêm từ MauiProgram.cs
        private readonly HttpClient _httpClient;

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
        }

        public async Task<List<Ban>> GetTablesAsync()
        {
            try
            {
                // 1. Lấy URL từ ApiConfig
                var url = ApiConfig.GetAllTables;

                // 2. Gọi API
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode(); // Ném lỗi nếu thất bại

                // 3. Đọc và Deserialize
                var json = await response.Content.ReadAsStringAsync();

                // Cần file Models/Ban.cs có [JsonPropertyName]
                return JsonSerializer.Deserialize<List<Ban>>(json) ?? new List<Ban>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[API GET LỖI] {ex.Message}");
                return new List<Ban>(); // Trả về rỗng nếu lỗi
            }
        }

        public async Task<bool> UpdateTableStatusAsync(string maBan, string newStatus)
        {
            try
            {
                // 1. Lấy URL từ ApiConfig
                var url = ApiConfig.UpdateTableStatus(maBan);

                // 2. Tạo Request Body (giống hệt Swagger)
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(newStatus),
                    Encoding.UTF8,
                    "application/json"
                );

                // 3. Gọi API
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