using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Controls;

namespace RestaurantManagementGUI
{
    public partial class AddUserPage : ContentPage
    {
        private readonly HttpClient _httpClient;

        public AddUserPage()
        {
            InitializeComponent();

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7004/") 
            };
        }

        private async void OnAddUserClicked(object sender, EventArgs e)
        {
            try
            {
                // Lấy dữ liệu từ form
                var user = new
                {
                    TenDangNhap = UsernameEntry.Text?.Trim(),
                    MatKhau = PasswordEntry.Text?.Trim(),
                    HoTen = HoTenEntry.Text?.Trim(),
                    ChucVu = ChucVuEntry.Text?.Trim(),
                    SDT = SDTEntry.Text?.Trim(),
                    Quyen = QuyenPicker.SelectedItem?.ToString() ?? "NhanVien"
                };

                // Kiểm tra dữ liệu nhập
                if (string.IsNullOrWhiteSpace(user.TenDangNhap) || string.IsNullOrWhiteSpace(user.MatKhau))
                {
                    await DisplayAlert("Thiếu thông tin", "Vui lòng nhập tên đăng nhập và mật khẩu.", "OK");
                    return;
                }

                // Gửi request POST /api/Auth/register
                var response = await _httpClient.PostAsJsonAsync("api/Auth/register", user);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<RegisterResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    await DisplayAlert("Thành công",
                        $"Thêm nhân viên thành công!\nMã NV: {data?.MaNV ?? "(chưa rõ)"}","OK");

                    // Xóa nội dung form sau khi thêm xong
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
