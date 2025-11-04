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
                // Xóa SecureStorage
                SecureStorage.RemoveAll();

                // Quay l?i trang Login
                Application.Current.MainPage = new NavigationPage(new LoginPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("L?i", $"??ng xu?t th?tt b?i: {ex.Message}", "OK");
            }
        }

        private async void OnOrdersClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new OrdersPage());

        private async void OnUsersClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new UsersPage());
    }
}
