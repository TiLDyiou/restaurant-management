using Microsoft.Maui.Devices;

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

        private async void OnUsersClicked(object sender, EventArgs e) => await Navigation.PushAsync(new UsersPage());
        private async void OnFoodCategoriesClicked(object sender, EventArgs e) => await Navigation.PushAsync(new QuanLyMonAnPage());
        private async void OnFoodMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new FoodMenuPage());
        private async void OnOrdersClicked(object sender, EventArgs e) => await Navigation.PushAsync(new OrdersPage());
        private async void OnTablesClicked(object sender, EventArgs e) => await Navigation.PushAsync(new TablesPage());
        private async void OnBillGenerationClicked(object sender, EventArgs e) => await Navigation.PushAsync(new BillGenerationPage());
        private async void OnOrderModificationsClicked(object sender, EventArgs e) => await Navigation.PushAsync(new OrderModificationsPage());

        protected override void OnAppearing()
        {
            base.OnAppearing();
            AdjustButtonSizes();
        }
        private void AdjustButtonSizes()
        {
            var screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;

            // Cột trái 160px, ScrollView padding + margin tổng cộng ~60
            double availableWidth = screenWidth - 160 - 60;

            double buttonWidth;

            if (screenWidth >= 1000) // Desktop → 4 cột
                buttonWidth = availableWidth / 4;
            else // Android → 2 cột
                buttonWidth = availableWidth / 2;

            foreach (var child in ButtonsLayout.Children)
            {
                if (child is Border border)
                {
                    border.WidthRequest = buttonWidth;

                    // Nút bên trong rộng đầy Border và tăng chiều cao
                    if (border.Content is VerticalStackLayout stack && stack.Children.Count > 1 && stack.Children[1] is Button btn)
                    {
                        btn.WidthRequest = buttonWidth - 20; // margin
                        btn.HeightRequest = 80; // tăng chiều cao để chữ không bị cắt
                        btn.FontSize = 14; // giảm font nếu cần
                    }
                }
            }
        }
    }
}
