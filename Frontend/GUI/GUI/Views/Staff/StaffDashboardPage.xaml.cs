using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Services;
using System.Net.Http.Headers;

namespace RestaurantManagementGUI.Views.Staff
{
    public partial class StaffDashboardPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        public StaffDashboardPage()
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
                string username = await SecureStorage.Default.GetAsync("user_username") ?? "Staff";
                WelcomeLabel.Text = $"Xin chào, {username}";
                UserState.CurrentRole = "Staff";
            }
            catch { }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Đăng xuất", "Bạn có chắc chắn muốn đăng xuất?", "Có", "Không");
            if (!confirm) return;

            if (sender is Button btn)
            {
                btn.IsEnabled = false;
                btn.Text = "Đang thoát...";
            }

            try
            {
                if (TCPSocketClient.Instance != null)
                {
                    await TCPSocketClient.Instance.DisconnectAsync();
                }

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

        private async void OnUserProfileClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ChefAndUserProfilePage());
        private async void OnFoodMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new FoodMenuPage());
        private async void OnOrdersClicked(object sender, EventArgs e) => await Navigation.PushAsync(new OrdersPage());
        private async void OnTablesClicked(object sender, EventArgs e) => await Navigation.PushAsync(new TablesPage());
        private async void OnBillGenerationClicked(object sender, EventArgs e) => await Navigation.PushAsync(new BillGenerationPage());
        private async void OnChatClicked(object sender, EventArgs e)
        {
            try
            {
                var chatPage = Handler.MauiContext.Services.GetService<ChatPage>();

                if (chatPage != null)
                {
                    await Navigation.PushAsync(chatPage);
                }
                else
                {
                    await DisplayAlert("Lỗi", "Hệ thống chưa đăng ký ChatPage!", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi khởi tạo", ex.Message, "OK");
                Console.WriteLine($"DEBUG: {ex.StackTrace}");
            }
        }
    }
}