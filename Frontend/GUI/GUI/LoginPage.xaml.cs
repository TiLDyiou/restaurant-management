using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Helpers;

namespace RestaurantManagementGUI
{
    public partial class LoginPage : ContentPage
    {
        private readonly HttpClient _httpClient;

        public LoginPage()
        {
            InitializeComponent();

#if DEBUG
            _httpClient = new HttpClient(GetInsecureHandler());
#else
            _httpClient = new HttpClient();
#endif
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string user = UsernameEntry.Text?.Trim() ?? string.Empty;
            string pass = PasswordEntry.Text ?? string.Empty;

            if (string.IsNullOrEmpty(user) && string.IsNullOrEmpty(pass))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập tên đăng nhập và mật khẩu.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(user))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập tên đăng nhập.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(pass))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập mật khẩu.", "OK");
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

                var response = await _httpClient.PostAsync(ApiConfig.Login, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = JsonSerializer.Deserialize<LoginResponseModel>(responseBody);
                    if (loginResponse != null && !string.IsNullOrWhiteSpace(loginResponse.Token))
                    {
                        await SecureStorage.Default.SetAsync("auth_token", loginResponse.Token);
                        await SecureStorage.Default.SetAsync("user_username", loginResponse.Username ?? loginResponse.MaNV);
                        await SecureStorage.Default.SetAsync("user_manv", loginResponse.MaNV);
                        await SecureStorage.Default.SetAsync("user_role", loginResponse.Role);
                        await SecureStorage.Default.SetAsync("user_chucvu", loginResponse.ChucVu ?? "");

                        string chucVu = loginResponse.ChucVu?.Trim().ToLower() ?? "";
                        if (chucVu == "đầu bếp" || chucVu == "dau bep")
                            Application.Current.MainPage = new NavigationPage(new ChefDashboardPage());
                        else
                            Application.Current.MainPage = new NavigationPage(new DashboardPage());
                    }
                }
                else
                {
                    // API trả lỗi chi tiết (chưa kích hoạt, chưa xác thực, sai mật khẩu)
                    await DisplayAlert("Lỗi", responseBody, "OK");
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
