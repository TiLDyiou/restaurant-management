using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.Views;
using RestaurantManagementGUI.Views.Staff;

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

            SecureStorage.RemoveAll();
            UserState.Clear();
            if (SocketListener.Instance != null)
                SocketListener.Instance.Disconnect();

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