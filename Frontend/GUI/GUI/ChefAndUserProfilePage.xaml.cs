using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace RestaurantManagementGUI
{
    public partial class ChefAndUserProfilePage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private UserModel _user = new();

        public ChefAndUserProfilePage()
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

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadProfileAsync();
        }

        private async Task LoadProfileAsync()
        {
            try
            {
                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (string.IsNullOrEmpty(token))
                {
                    await DisplayAlert("Lỗi", "Chưa đăng nhập hoặc token hết hạn!", "OK");
                    return;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _httpClient.GetAsync(ApiConfig.Me);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _user = JsonSerializer.Deserialize<UserModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new UserModel();

                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    _user.TenDangNhap ??= root.GetProperty("username").GetString();
                    _user.Quyen ??= root.GetProperty("role").GetString();
                    _user.MaNV ??= root.GetProperty("maNV").GetString();
                    _user.ChucVu ??= root.GetProperty("chucVu").GetString();
                    _user.SDT ??= root.TryGetProperty("sdt", out var sdtProp) ? sdtProp.GetString() : "";
                }
                else
                {
                    _user.TenDangNhap = await SecureStorage.Default.GetAsync("user_username");
                    _user.MaNV = await SecureStorage.Default.GetAsync("user_manv");
                    _user.Quyen = await SecureStorage.Default.GetAsync("user_role");
                    _user.ChucVu = await SecureStorage.Default.GetAsync("user_chucvu");
                    _user.HoTen = _user.TenDangNhap;
                }

                MaNVLabel.Text = _user.MaNV;
                HoTenLabel.Text = _user.HoTen;
                ChucVuLabel.Text = _user.ChucVu;
                QuyenLabel.Text = _user.Quyen;
                TenDangNhapLabel.Text = _user.TenDangNhap;
                TrangThaiLabel.Text = _user.TrangThai;
                SDTLabel.Text = _user.SDT;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Không thể lấy thông tin: {ex.Message}", "OK");
            }
        }

        private async void OnEditProfileClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new EditChefAndUserProfilePage(_user));
        }
    }
}
