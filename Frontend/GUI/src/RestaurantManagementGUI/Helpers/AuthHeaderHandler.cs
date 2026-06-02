using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Models;

namespace RestaurantManagementGUI.Helpers
{
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly JsonSerializerOptions _jsonOptions;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public AuthHeaderHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Bỏ qua việc đính kèm header đối với các API Auth công khai
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("/api/auth/login") || 
                path.Contains("/api/auth/register") || 
                path.Contains("/api/auth/otp/") || 
                path.Contains("/api/auth/verify/") ||
                path.Contains("/api/auth/forgot-password") ||
                path.Contains("/api/auth/reset-password"))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            // Nạp token từ SecureStorage nếu chưa có trong UserState (lần đầu mở app hoặc bị khôi phục state)
            if (string.IsNullOrEmpty(UserState.AccessToken))
            {
                UserState.AccessToken = await SecureStorage.Default.GetAsync("auth_token") ?? "";
            }
            if (string.IsNullOrEmpty(UserState.RefreshToken))
            {
                UserState.RefreshToken = await SecureStorage.Default.GetAsync("refresh_token") ?? "";
            }

            // Đính kèm JWT Access Token vào header Authorization
            if (!string.IsNullOrEmpty(UserState.AccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", UserState.AccessToken);
            }

            var response = await base.SendAsync(request, cancellationToken);

            // Bắt lỗi 401 Unauthorized -> Thực hiện cơ chế xoay vòng Refresh Token tự động
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                bool isRefreshed = await TryRefreshTokenAsync();
                if (isRefreshed)
                {
                    // Ghi đè header Authorization bằng token mới vừa lấy được
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", UserState.AccessToken);
                    
                    // Thử gửi lại request gốc bị lỗi
                    return await base.SendAsync(request, cancellationToken);
                }
                else
                {
                    // Nếu Refresh Token cũng hết hạn -> Trả về màn hình đăng nhập
                    await NavigateToLoginAsync();
                }
            }

            return response;
        }

        private async Task<bool> TryRefreshTokenAsync()
        {
            // Sử dụng Semaphore để tránh nhiều API đồng loạt chạy tiến trình refresh cùng lúc
            await _semaphore.WaitAsync();
            try
            {
                var refreshToken = UserState.RefreshToken;
                if (string.IsNullOrEmpty(refreshToken))
                {
                    refreshToken = await SecureStorage.Default.GetAsync("refresh_token");
                }

                if (string.IsNullOrEmpty(refreshToken)) return false;

                // Tạo một HttpClient sạch độc lập (để tránh bị đính kèm handler gây lặp vô hạn)
                var cleanHandler = HttpsClientHandlerService.GetPlatformMessageHandler();
                using var cleanClient = new HttpClient(cleanHandler) { BaseAddress = new Uri(ApiConfig.BaseUrl) };

                var refreshRequest = new { RefreshToken = refreshToken };
                var response = await cleanClient.PostAsJsonAsync("api/auth/refresh", refreshRequest);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponseModel>>(responseBody, _jsonOptions);

                    if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                    {
                        var data = apiResponse.Data;

                        // Lưu trữ bộ token mới
                        await SecureStorage.Default.SetAsync("auth_token", data.AccessToken);
                        await SecureStorage.Default.SetAsync("refresh_token", data.RefreshToken ?? "");
                        
                        UserState.AccessToken = data.AccessToken;
                        UserState.RefreshToken = data.RefreshToken;

                        Console.WriteLine("🔑 [CI/CD Client] Tự động xoay vòng Refresh Token thành công!");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [CI/CD Client] Tiến trình Refresh Token thất bại: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }

            return false;
        }

        private async Task NavigateToLoginAsync()
        {
            // Xóa sạch trạng thái đăng nhập
            UserState.Clear();
            SecureStorage.Default.RemoveAll();

            // Chuyển hướng người dùng về trang đăng nhập trên luồng UI chính (Main Thread)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var loginPage = _serviceProvider.GetService<LoginPage>();
                    if (loginPage != null)
                    {
                        Application.Current.MainPage = new NavigationPage(loginPage);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ [CI/CD Client] Không thể điều hướng về trang đăng nhập: {ex.Message}");
                }
            });

            await Task.CompletedTask;
        }
    }
}
