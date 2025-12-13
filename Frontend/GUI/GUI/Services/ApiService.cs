using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;

namespace RestaurantManagementGUI.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IUserSession _userSession;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService(HttpClient httpClient, IUserSession userSession)
        {
            _httpClient = httpClient;
            _userSession = userSession;

            // Cấu hình HttpClient cơ sở
            _httpClient.BaseAddress = new Uri(ApiConfig.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        // Hàm generic GET
        public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            AddAuthorizationHeader();
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                return await HandleResponse<T>(response);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<T>(ex.Message);
            }
        }

        // Hàm generic POST
        public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object payload)
        {
            AddAuthorizationHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, payload);
                return await HandleResponse<T>(response);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<T>(ex.Message);
            }
        }

        // Hàm generic PUT
        public async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object payload)
        {
            AddAuthorizationHeader();
            try
            {
                var response = await _httpClient.PutAsJsonAsync(endpoint, payload);
                return await HandleResponse<T>(response);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<T>(ex.Message);
            }
        }

        // Hàm generic DELETE
        public async Task<ApiResponse<T>> DeleteAsync<T>(string endpoint)
        {
            AddAuthorizationHeader();
            try
            {
                var response = await _httpClient.DeleteAsync(endpoint);
                return await HandleResponse<T>(response);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<T>(ex.Message);
            }
        }

        // --- PRIVATE HELPERS ---

        private void AddAuthorizationHeader()
        {
            if (_userSession.IsAuthenticated)
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _userSession.Token);
            }
        }

        private async Task<ApiResponse<T>> HandleResponse<T>(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                // Cố gắng đọc lỗi từ Backend trả về
                try
                {
                    var errorContent = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(_jsonOptions);
                    if (errorContent != null) return errorContent;
                }
                catch { } // Nếu không đọc được JSON lỗi thì bỏ qua

                return new ApiResponse<T>
                {
                    Success = false,
                    Message = $"Lỗi Server: {response.StatusCode}"
                };
            }

            try
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(_jsonOptions);
                return result ?? new ApiResponse<T> { Success = false, Message = "Dữ liệu trống" };
            }
            catch
            {
                return new ApiResponse<T> { Success = false, Message = "Lỗi phân tích dữ liệu" };
            }
        }

        private ApiResponse<T> CreateErrorResponse<T>(string message)
        {
            return new ApiResponse<T> { Success = false, Message = message };
        }

    }
}