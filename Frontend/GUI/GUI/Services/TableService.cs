using System.Net.Http.Json;
using System.Text.Json;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;

namespace RestaurantManagementGUI.Services
{
    public class TableService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public TableService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<List<Ban>> GetAllTablesAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<Ban>>>(ApiConfig.Tables, _jsonOptions);
                return response?.Data ?? new List<Ban>();
            }
            catch { return new List<Ban>(); }
        }

        public async Task<bool> UpdateStatusAsync(string maBan, string trangThai)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(ApiConfig.UpdateTableStatus(maBan), trangThai);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}