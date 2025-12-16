using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Services;
namespace RestaurantManagementGUI.Views.Staff
{
    public partial class StaffDashboardPage : ContentPage
    {
        public StaffDashboardPage()
        {
            InitializeComponent();
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

        private async void OnUserProfileClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ChefAndUserProfilePage());
        private async void OnFoodMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new FoodMenuPage());
        private async void OnOrdersClicked(object sender, EventArgs e) => await Navigation.PushAsync(new OrdersPage());
        private async void OnTablesClicked(object sender, EventArgs e) => await Navigation.PushAsync(new TablesPage());
        private async void OnBillGenerationClicked(object sender, EventArgs e) => await Navigation.PushAsync(new BillGenerationPage());
        private async void OnChatClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ChatPage());
    }
}