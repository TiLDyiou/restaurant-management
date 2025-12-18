using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.Views;
using RestaurantManagementGUI.Views.Staff;
using System.Net.Http.Headers;

namespace RestaurantManagementGUI
{
    public partial class DashboardPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        public DashboardPage()
        {
            InitializeComponent();
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
            _httpClient = new HttpClient(handler);
            LoadUserInfo();
        }

        private async void LoadUserInfo()
        {
            try
            {
                string tenNV = await SecureStorage.Default.GetAsync("user_username") ?? "Admin";
                WelcomeLabel.Text = $"Xin chào, {tenNV}";
                UserState.CurrentTenNV = tenNV;
                UserState.CurrentMaNV = await SecureStorage.Default.GetAsync("user_manv") ?? "";
                UserState.CurrentRole = "Admin";
            }
            catch { }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Đăng xuất", "Bạn có chắc chắn muốn đăng xuất?", "Có", "Không");
            if (!confirm) return;
            LogoutButton.IsEnabled = false;
            LogoutButton.Text = "Đang thoát...";

            try
            {
                // 1. Gửi lệnh LOGOUT qua Socket để Server báo Offline ngay lập tức cho các máy khác
                if (Services.TCPSocketClient.Instance != null)
                {
                    await Services.TCPSocketClient.Instance.DisconnectAsync();
                }

                // 2. Gọi API Logout để Backend xóa Token/Cookie và cập nhật DB (Persistent)
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
            SecureStorage.RemoveAll();
            UserState.Clear();
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }

        private async void OnUsersClicked(object sender, EventArgs e) => await Navigation.PushAsync(new UsersPage());
        private async void OnFoodCategoriesClicked(object sender, EventArgs e) => await Navigation.PushAsync(new QuanLyMonAnPage());
        private async void OnFoodMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new FoodMenuPage());
        private async void OnOrdersClicked(object sender, EventArgs e) => await Navigation.PushAsync(new OrdersPage());
        private async void OnTablesClicked(object sender, EventArgs e) => await Navigation.PushAsync(new TablesPage());
        private async void OnBillGenerationClicked(object sender, EventArgs e) => await Navigation.PushAsync(new BillGenerationPage());
        private async void OnRevenueReportClicked(object sender, EventArgs e) => await Navigation.PushAsync(new RevenueReportPage());
        private async void OnChatClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ChatPage());
    }
}