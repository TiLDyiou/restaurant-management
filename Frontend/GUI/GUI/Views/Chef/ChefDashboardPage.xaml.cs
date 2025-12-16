using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.Views;

namespace RestaurantManagementGUI
{
    public partial class ChefDashboardPage : ContentPage
    {
        public ChefDashboardPage()
        {
            InitializeComponent();
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

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Đăng xuất", "Bạn có chắc chắn muốn đăng xuất?", "Có", "Không");
            if (!confirm) return;

            // 1. Ngắt kết nối TCP (Gửi LOGOUT lên Server để Server báo Offline)
            if (Services.SocketListener.Instance != null)
            {
                Services.SocketListener.Instance.Disconnect();
            }

            // 2. Sau đó mới xóa dữ liệu máy
            SecureStorage.RemoveAll();
            UserState.Clear();

            // 3. Chuyển trang
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }

        private async void OnOrdersClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ChefOrdersPage());
        private async void OnFoodMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new FoodMenuPage());

        private async void OnUsersClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ChefAndUserProfilePage());
    }
}