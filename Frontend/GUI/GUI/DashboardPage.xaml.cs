using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Views;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.ViewModels;
using Microsoft.Maui;
using Microsoft.Extensions.DependencyInjection;


namespace RestaurantManagementGUI
{
    public partial class DashboardPage : ContentPage
    {
        public DashboardPage()
        {
            InitializeComponent();
            LoadUserInfo();
        }

        private async void LoadUserInfo()
        {
            try
            {
                string username = await SecureStorage.Default.GetAsync("user_username") ?? "User";
                WelcomeLabel.Text = $"Welcome, {username}";
            }
            catch
            {
                WelcomeLabel.Text = "Welcome!";
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                SecureStorage.RemoveAll();
                Application.Current.MainPage = new NavigationPage(new LoginPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Đăng xuất thất bại: {ex.Message}", "OK");
            }
        }

        // Users: admin mở UsersPage, còn các chức vụ khác mở profile
        private async void OnUsersClicked(object sender, EventArgs e)
        {
            try
            {
                var role = await SecureStorage.Default.GetAsync("user_role") ?? "User";

                if (role.Trim().ToLower() == "admin")
                    await Navigation.PushAsync(new UsersPage());
                else
                    await Navigation.PushAsync(new ChefAndUserProfilePage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Không thể mở trang: {ex.Message}", "OK");
            }
        }
        private async void OnFoodCategoriesClicked(object sender, EventArgs e) => await Navigation.PushAsync(new QuanLyMonAnPage());
        private async void OnFoodMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new FoodMenuPage());
        private async void OnOrdersClicked(object sender, EventArgs e) => await Navigation.PushAsync(new OrdersPage());
        private async void OnTablesClicked(object sender, EventArgs e)
        {
            try
            {
                // Lấy DI container chính xác trong MAUI
                var services = App.Current?.Handler?.MauiContext?.Services;

                TablesPage page = null;

                if (services != null)
                {
                    page = services.GetService<TablesPage>();
                }

                if (page == null)
                {
                    // fallback nếu DI không đăng ký
                    var apiService = new ApiService();
                    var tableHubService = new TableHubService();
                    var vm = new TablesViewModel(apiService, tableHubService);
                    page = new TablesPage(vm, apiService);
                }

                await Navigation.PushAsync(page);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Không thể mở trang bàn: {ex.Message}", "OK");
            }
        }


        private async void OnBillGenerationClicked(object sender, EventArgs e) => await Navigation.PushAsync(new BillGenerationPage());
        private async void OnChatClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ChatPage());

        protected override void OnAppearing()
        {
            base.OnAppearing();
            AdjustButtonSizes();
        }

        private void AdjustButtonSizes()
        {
            var screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
            double availableWidth = screenWidth - 160 - 60; // trừ cột trái + margin/padding
            double buttonWidth = screenWidth >= 1000 ? availableWidth / 4 : availableWidth / 2;

            foreach (var child in ButtonsLayout.Children)
            {
                if (child is Border border)
                {
                    border.WidthRequest = buttonWidth;

                    if (border.Content is VerticalStackLayout stack && stack.Children.Count > 1 && stack.Children[1] is Button btn)
                    {
                        btn.WidthRequest = buttonWidth - 20;
                        btn.HeightRequest = 80;
                        btn.FontSize = 14;
                    }
                }
            }
        }
    }
}