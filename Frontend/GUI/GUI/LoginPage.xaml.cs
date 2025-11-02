using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage; // Để lưu token (SecureStorage)
using RestaurantManagementGUI.Models; // Nơi chứa 2 model bạn vừa tạo

namespace RestaurantManagementGUI
{
    public partial class LoginPage : ContentPage
    {

        private readonly HttpClient _httpClient;

        public LoginPage()
        {
            InitializeComponent();

            // GIẢI QUYẾT VẤN ĐỀ SSL VÀ LOCALHOST
#if DEBUG
            // Chỉ trong chế độ DEBUG, chúng ta mới bỏ qua lỗi chứng chỉ
            // ĐỪNG BAO GIỜ dùng code này trong sản phẩm thật (production)
            HttpClientHandler insecureHandler = GetInsecureHandler();
            _httpClient = new HttpClient(insecureHandler);
#else
            // Khi build release, dùng HttpClient bình thường
            _httpClient = new HttpClient();
#endif
        }

        // Hàm này trả về URL đúng cho từng nền tảng
        private string GetApiUrl()
        {
            // Nếu chạy trên Windows, dùng localhost
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                return "https://localhost:7004/api/Auth/login";
            }

            // Nếu chạy trên Android, dùng IP đặc biệt 10.0.2.2
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                return "https://10.0.2.2:7004/api/Auth/login";
            }

            // Các nền tảng khác (iOS, Mac) sẽ cần IP LAN của máy bạn, 
            // nhưng hiện tại chúng ta chỉ xử lý 2 trường hợp này
            return "https://localhost:7004/api/Auth/login";
        }

        // Hàm chính xử lý đăng nhập
        async void OnLoginClicked(object sender, EventArgs e)
        {
            string user = UsernameEntry.Text?.Trim() ?? string.Empty;
            string pass = PasswordEntry.Text ?? string.Empty;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập tên đăng nhập và mật khẩu.", "OK");
                return;
            }

            // Vô hiệu hóa nút để tránh nhấn 2 lần
            LoginButton.IsEnabled = false;
            LoginButton.Text = "Đang xử lý...";

            try
            {
                // Tạo đối tượng request
                var loginRequest = new LoginRequestModel
                {
                    TenDangNhap = user,
                    MatKhau = pass
                };

                // Chuyển sang JSON
                string jsonPayload = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Lấy URL đúng (localhost hoặc 10.0.2.2)
                string apiUrl = GetApiUrl();

                //  Gửi yêu cầu POST đến API
                HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

                // Xử lý kết quả
                if (response.IsSuccessStatusCode)
                {
                    // Đọc nội dung JSON trả về
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponseModel>(responseBody);

                    // Lưu token bảo mật
                    if (loginResponse != null && !string.IsNullOrWhiteSpace(loginResponse.Token))
                    {
                        await SecureStorage.Default.SetAsync("auth_token", loginResponse.Token);
                        await SecureStorage.Default.SetAsync("user_username", loginResponse.Username ?? loginResponse.MaNV);
                        await SecureStorage.Default.SetAsync("user_manv", loginResponse.MaNV);
                        await SecureStorage.Default.SetAsync("user_role", loginResponse.Role);
                        await SecureStorage.Default.SetAsync("user_chucvu", loginResponse.ChucVu ?? "");


                        // Chuyển hướng Dashboard theo chức vụ
                        string chucVu = loginResponse.ChucVu?.Trim().ToLower() ?? "";

                        if (chucVu == "đầu bếp" || chucVu == "dau bep")
                        {
                            Application.Current.MainPage = new NavigationPage(new ChefDashboardPage());
                        }
                        else
                        {
                            Application.Current.MainPage = new NavigationPage(new DashboardPage());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Lỗi kết nối (Ví dụ: API chưa chạy, mất mạng, lỗi SSL...)
                await DisplayAlert("Lỗi kết nối", $"Không thể kết nối đến máy chủ: {ex.Message}", "OK");
            }
            finally
            {
                // Kích hoạt lại nút bấm
                LoginButton.IsEnabled = true;
                LoginButton.Text = "Đăng nhập";
            }
        }

        // Hàm xử lý "Quên mật khẩu"
        async void OnForgotPasswordClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Quên mật khẩu", "Vui lòng liên hệ quản trị viên.", "OK");
        }

        // Hàm trợ giúp để bỏ qua lỗi SSL (Chỉ dùng cho DEBUG)
        public HttpClientHandler GetInsecureHandler()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
            {
                if (sender is HttpRequestMessage request)
                {
                    // Tự động tin tưởng "localhost" hoặc "10.0.2.2"
                    return request.RequestUri.IsLoopback ||
                           (DeviceInfo.Platform == DevicePlatform.Android && request.RequestUri.Host == "10.0.2.2");
                }
                return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
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