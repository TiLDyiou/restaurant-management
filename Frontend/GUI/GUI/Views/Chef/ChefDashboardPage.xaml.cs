using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Services;

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
                bool confirm = await DisplayAlert("Đăng xuất", "Bạn có chắc chắn muốn đăng xuất?", "Có", "Không");
                if (!confirm) return;
                SecureStorage.RemoveAll();
                // Ngắt kết nối Socket
                SocketListener.Instance.Disconnect();

                Application.Current.MainPage = new NavigationPage(new LoginPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Đăng xuất thất bại: {ex.Message}", "OK");
            }
        }

        private async void OnOrdersClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new ChefOrdersPage());

        private async void OnUsersClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new ChefAndUserProfilePage());
    }
}
