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
                // Xóa SecureStorage
                SecureStorage.RemoveAll();

                // Quay l?i trang Login
                Application.Current.MainPage = new NavigationPage(new LoginPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("L?i", $"??ng xu?t th?t b?i: {ex.Message}", "OK");
            }
        }

        private async void OnRolesClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new RolesPage());

        private async void OnUsersClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new UsersPage());

        private async void OnFoodCategoriesClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new FoodCategoriesPage());

        private async void OnFoodMenuClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new FoodMenuPage());

        private async void OnOrdersClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new OrdersPage());

        private async void OnTablesClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new TablesPage());

        private async void OnBillGenerationClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new BillGenerationPage());

        private async void OnOrderModificationsClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new OrderModificationsPage());
    }
}
