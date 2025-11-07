using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;

namespace RestaurantManagementGUI
{
    public partial class AddUserPage : ContentPage
    {
        private readonly HttpClient _httpClient;

        public AddUserPage()
        {
            InitializeComponent();

#if DEBUG
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) =>
                    msg?.RequestUri?.Host == "10.0.2.2" || msg?.RequestUri?.IsLoopback == true
            };
            _httpClient = new HttpClient(handler);
#else
            _httpClient = new HttpClient();
#endif

            _httpClient.BaseAddress = new Uri(ApiConfig.BaseUrl);
        }

        private async void OnAddUserClicked(object sender, EventArgs e)
        {
            try
            {
                var newUser = new AddUserRequestModel
                {
                    TenDangNhap = UsernameEntry.Text?.Trim(),
                    MatKhau = PasswordEntry.Text?.Trim(),
                    HoTen = HoTenEntry.Text?.Trim(),
                    ChucVu = ChucVuEntry.Text?.Trim(),
                    SDT = SDTEntry.Text?.Trim(),
                    Quyen = QuyenPicker.SelectedItem?.ToString() ?? "NhanVien"
                };

                if (string.IsNullOrWhiteSpace(newUser.TenDangNhap) || string.IsNullOrWhiteSpace(newUser.MatKhau))
                {
                    await DisplayAlert("Thiếu thông tin", "Vui lòng nhập tên đăng nhập và mật khẩu.", "OK");
                    return;
                }

                // Gắn token nếu API cần xác thực
                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // Gọi endpoint Register từ ApiConfig
                var response = await _httpClient.PostAsJsonAsync(ApiConfig.Register, newUser);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<RegisterResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    await DisplayAlert("Thành công",
                        $"Thêm nhân viên thành công!\nMã NV: {data?.MaNV ?? "(chưa rõ)"}",
                        "OK");

                    // Xóa form sau khi thêm
                    UsernameEntry.Text = PasswordEntry.Text = HoTenEntry.Text = ChucVuEntry.Text = SDTEntry.Text = "";
                    QuyenPicker.SelectedIndex = -1;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Lỗi", $"Không thể thêm nhân viên!\n{error}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Đã xảy ra lỗi: {ex.Message}", "OK");
            }
        }
    }
}
