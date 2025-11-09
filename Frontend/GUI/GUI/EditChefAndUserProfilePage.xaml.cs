using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace RestaurantManagementGUI
{
    public partial class EditChefAndUserProfilePage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly UserModel _user;

        public EditChefAndUserProfilePage(UserModel user)
        {
            InitializeComponent();
            _user = user;

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

            SDTEntry.Text = _user.SDT;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (string.IsNullOrEmpty(token))
                {
                    await DisplayAlert("Lỗi", "Chưa Đăng nhập hoặc token hết hạn!", "OK");
                    return;
                }

                string newPassword = PasswordEntry.Text?.Trim();
                string currentPassword = CurrentPasswordEntry.Text?.Trim();

                if (!string.IsNullOrWhiteSpace(newPassword) && string.IsNullOrWhiteSpace(currentPassword))
                {
                    await DisplayAlert("Lỗi", "Vui lòng nhập mật khẩu hiện tại!", "OK");
                    return;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var payload = new
                {
                    sdt = SDTEntry.Text?.Trim(),
                    currentPassword = string.IsNullOrWhiteSpace(newPassword) ? null : currentPassword,
                    newPassword = string.IsNullOrWhiteSpace(newPassword) ? null : newPassword
                };

                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(ApiConfig.UpdateProfile, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    _user.SDT = root.TryGetProperty("sdt", out var sdtProp) ? sdtProp.GetString() : _user.SDT;

                    await DisplayAlert("Thành công", "Cập nhật thông tin cá nhân thành công!", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    string err = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Lỗi", $"Cập nhật thất bại: {err}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi kết nối", $"Không thể kết nối: {ex.Message}", "OK");
            }
        }
    }
}
