using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using System.Net.Http.Json;
using Microsoft.Maui.Storage;

namespace RestaurantManagementGUI
{
    public partial class EditUserPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly string _maNV;

        public EditUserPage(UserModel user)
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
            _maNV = user.MaNV;

            // Hiển thị thông tin hiện tại
            MaNVLabel.Text = $"Mã nhân viên: {user.MaNV}";
            HoTenEntry.Text = user.HoTen;
            ChucVuEntry.Text = user.ChucVu;
            SDTEntry.Text = user.SDT;
            QuyenPicker.SelectedItem = user.Quyen;
        }

        private async void OnUpdateUserClicked(object sender, EventArgs e)
        {
            try
            {
                // Tạo DTO gửi lên API
                var updateUser = new UpdateUserRequestModel
                {
                    HoTen = HoTenEntry.Text?.Trim(),
                    ChucVu = ChucVuEntry.Text?.Trim(),
                    SDT = SDTEntry.Text?.Trim(),
                    Quyen = QuyenPicker.SelectedItem?.ToString()?.Trim()
                };

                // Lấy token từ SecureStorage nếu có
                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // Gọi API admin-update/{maNV}
                var response = await _httpClient.PutAsJsonAsync($"Auth/admin-update/{_maNV}", updateUser);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Thành công", "Cập nhật nhân viên thành công!", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Lỗi", $"Không thể cập nhật nhân viên: {error}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Không thể kết nối: {ex.Message}", "OK");
            }
        }
    }
}
