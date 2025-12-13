using RestaurantManagementGUI.ViewModels;

namespace RestaurantManagementGUI.Views.Authentication
{
    public partial class LoginPage : ContentPage
    {
        // Tiêm ViewModel vào Constructor
        public LoginPage(LoginViewModel viewModel)
        {
            InitializeComponent();
            this.BindingContext = viewModel;
        }
    }
}