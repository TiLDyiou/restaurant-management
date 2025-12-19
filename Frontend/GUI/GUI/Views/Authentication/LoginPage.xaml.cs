using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.Views;
using RestaurantManagementGUI.Views.Staff;

namespace RestaurantManagementGUI
{
    public partial class LoginPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public LoginPage()
        {
            InitializeComponent();

#if DEBUG
            _httpClient = new HttpClient(GetInsecureHandler());
#else
            _httpClient = new HttpClient();
#endif
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string user = UsernameEntry.Text?.Trim() ?? string.Empty;
            string pass = PasswordEntry.Text ?? string.Empty;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.", "OK");
                return;
            }

            LoginButton.IsEnabled = false;
            LoginButton.Text = "Đang xử lý...";

            try
            {
                var loginRequest = new LoginRequestModel
                {
                    TenDangNhap = user,
                    MatKhau = pass
                };

                string jsonPayload = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // GỌI API
                var response = await _httpClient.PostAsync(ApiConfig.Login, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                var apiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponseModel>>(responseBody, _jsonOptions);

                if (response.IsSuccessStatusCode && apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                {
                    var data = apiResponse.Data;

                    if (!string.IsNullOrWhiteSpace(data.Token))
                    {
                        
                        await SecureStorage.Default.SetAsync("auth_token", data.Token);
                        await SecureStorage.Default.SetAsync("user_username", data.Username ?? data.MaNV);
                        await SecureStorage.Default.SetAsync("user_manv", data.MaNV);
                        await SecureStorage.Default.SetAsync("user_role", data.Role ?? "");

                        
                        if (!string.IsNullOrEmpty(data.MaNV))
                        {
                            await SecureStorage.Default.SetAsync("current_ma_nv", data.MaNV);
                        }

                        // Cập nhật biến toàn cục
                        UserState.CurrentMaNV = data.MaNV;
                        UserState.CurrentTenNV = data.Username;
                        UserState.CurrentRole = data.Role;
                        UserState.AccessToken = data.Token;

                        // Gọi sau khi đã lưu xong current_ma_nv
                        if (!string.IsNullOrEmpty(data.MaNV))
                        {
                            await Services.TCPSocketClient.Instance.LoginAsync(data.MaNV);
                        }
                        string role = data.Role?.Trim().ToLower() ?? "";

                        if (role == "admin")
                        {
                            Application.Current.MainPage = new NavigationPage(new DashboardPage());
                        }
                        else if (role == "daubep")
                        {
                            Application.Current.MainPage = new NavigationPage(new ChefDashboardPage());
                        }
                        else
                        {
                            Application.Current.MainPage = new NavigationPage(new StaffDashboardPage());
                        }
                    }
                }
                else
                {
                    string msg = apiResponse?.Message ?? "Đăng nhập thất bại.";
                    await DisplayAlert("Lỗi", msg, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi kết nối", $"Không thể kết nối đến máy chủ: {ex.Message}", "OK");
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Text = "Đăng nhập";
            }
        }

        private async void OnForgotPasswordClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ForgotPasswordPage());
        }

        private HttpClientHandler GetInsecureHandler()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) =>
            {
                if (sender is HttpRequestMessage request)
                {
                    return request.RequestUri.IsLoopback ||
                           (DeviceInfo.Platform == DevicePlatform.Android && request.RequestUri.Host == "10.0.2.2");
                }
                return errors == System.Net.Security.SslPolicyErrors.None;
            };
            return handler;
        }

        private void OnPasswordToggleTapped(object sender, TappedEventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            PasswordToggleIcon.Source = PasswordEntry.IsPassword ? "eye_hide.png" : "eye_show.png";
        }
    }
}