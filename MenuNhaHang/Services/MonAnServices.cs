using MenuNhaHang.Models; // Sử dụng Model MonAn.cs
using System.Net.Http.Json;

namespace MenuNhaHang.Services
{
    public class MonAnService
    {
        private readonly HttpClient _httpClient;

        public MonAnService()
        {
            // Sử dụng BaseUrl từ file Constants
            _httpClient = new HttpClient { BaseAddress = new Uri(ApiConstants.BaseUrl) };
        }

        public async Task<List<MonAn>> GetMonAnAsync()
        {
            try
            {
                // Gọi API Endpoint
                var response = await _httpClient.GetAsync(ApiConstants.MonAnEndpoint);
                response.EnsureSuccessStatusCode();

                // Trả về danh sách MonAn từ API
                return await response.Content.ReadFromJsonAsync<List<MonAn>>();
            }
            catch (Exception ex)
            {
                // Xử lý lỗi
                Console.WriteLine($"Lỗi khi lấy món ăn: {ex.Message}");
                return new List<MonAn>();
            }
        }
    }
}