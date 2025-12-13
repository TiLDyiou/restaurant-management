using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.Views.Authentication; // Login
using RestaurantManagementGUI.Views.Chef; // ChefOrders

namespace RestaurantManagementGUI.Views.Chef
{
    public partial class ChefDashboardPage : ContentPage
    {
        private readonly IUserSession _userSession;

        public ChefDashboardPage()
        {
            InitializeComponent();
            // Lấy session thủ công (hoặc tiêm nếu đăng ký Transient)
            _userSession = IPlatformApplication.Current.Services.GetService<IUserSession>();
            LoadUserInfo();
        }

        private void LoadUserInfo()
        {
            if (_userSession != null)
            {
                WelcomeLabel.Text = $"Xin chào, {_userSession.TenNV}";
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Đăng xuất", "Bạn muốn đăng xuất?", "Có", "Không");
            if (!confirm) return;

            SecureStorage.RemoveAll();
            _userSession?.Clear();
            SocketListener.Instance.Disconnect();

            Application.Current.MainPage = new NavigationPage(new LoginPage(IPlatformApplication.Current.Services.GetService<ViewModels.LoginViewModel>()));
        }

        private async void OnOrdersClicked(object sender, EventArgs e)
        {
            // Lấy page từ DI để có ViewModel
            var page = IPlatformApplication.Current.Services.GetService<ChefOrdersPage>();
            await Navigation.PushAsync(page);
        }

        private async void OnUsersClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ChefAndUserProfilePage());
        }
    }
}