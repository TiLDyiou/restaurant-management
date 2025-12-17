using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.Views;
using System.Net.Http.Headers; // Quan trọng

namespace RestaurantManagementGUI
{
    public partial class ChefDashboardPage : ContentPage
    {
        private readonly HttpClient _httpClient; // 1. Thêm biến này

        public ChefDashboardPage()
        {
            InitializeComponent();

            // 2. Khởi tạo HttpClient
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
            _httpClient = new HttpClient(handler);

            LoadUserInfo();
        }

        private async void LoadUserInfo()
        {
            try
            {
                string username = await SecureStorage.Default.GetAsync("user_username") ?? "Chef";
                WelcomeLabel.Text = $"Đầu bếp: {username}";
                UserState.CurrentRole = "DauBep";
            }
            catch { }
        }

        // 3. Hàm Logout chuẩn
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Đăng xuất", "Bạn có chắc chắn muốn đăng xuất?", "Có", "Không");
            if (!confirm) return;

            // Xử lý UI
            if (sender is Button btn)
            {
                btn.IsEnabled = false;
                btn.Text = "Đang thoát...";
            }

            try
            {
                // Bước 1: Ngắt Socket
                if (SocketListener.Instance != null)
                {
                    await SocketListener.Instance.DisconnectAsync();
                }

                // Bước 2: Gọi API Logout
                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Post, ApiConfig.Logout))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        await _httpClient.SendAsync(request);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi đăng xuất: {ex.Message}");
            }

            // Bước 3: Cleanup & Redirect
            SecureStorage.RemoveAll();
            UserState.Clear();
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }

        private async void OnOrdersClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ChefOrdersPage());
        private async void OnFoodMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new FoodMenuPage());
        private async void OnUsersClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ChefAndUserProfilePage());
    }
}