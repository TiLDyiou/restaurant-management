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
            bool confirm = await DisplayAlert("Đăng xuất", "Đóng ca làm việc?", "Có", "Không");
            if (!confirm) return;

            SecureStorage.RemoveAll();
            UserState.Clear();
            if (SocketListener.Instance != null) SocketListener.Instance.Disconnect();
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }

        private async void OnOrdersClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ChefOrdersPage());
        private async void OnFoodMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new FoodMenuPage());

        private async void OnUsersClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ChefAndUserProfilePage());
    }
}