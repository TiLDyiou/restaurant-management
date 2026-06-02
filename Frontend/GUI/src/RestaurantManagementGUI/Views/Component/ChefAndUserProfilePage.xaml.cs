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
        private readonly JsonSerializerOptions _jsonOptions;
        private UserModel _user = new();

        public ChefAndUserProfilePage(HttpClient httpClient)
        {
            InitializeComponent();

            _httpClient = httpClient;

            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
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
                    await DisplayAlert("Lỗi", "Phiên đăng nhập hết hạn.", "OK");
                    await Navigation.PopToRootAsync();
                    return;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetFromJsonAsync<ApiResponse<UserModel>>(ApiConfig.Me, _jsonOptions);

                if (response != null && response.Success && response.Data != null)
                {
                    _user = response.Data;

                    MaNVLabel.Text = _user.MaNV;
                    HoTenLabel.Text = _user.HoTen;
                    ChucVuLabel.Text = _user.ChucVu;
                    QuyenLabel.Text = _user.Quyen;
                    TenDangNhapLabel.Text = _user.TenDangNhap;
                    SDTLabel.Text = _user.SDT;
                    EmailLabel.Text = _user.Email;
                }
                else
                {
                    await DisplayAlert("Lỗi", response?.Message ?? "Không tải được thông tin cá nhân", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi kết nối", ex.Message, "OK");
            }
        }

        private async void OnEditProfileClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new EditChefAndUserProfilePage(_httpClient, _user));
        }
    }
}