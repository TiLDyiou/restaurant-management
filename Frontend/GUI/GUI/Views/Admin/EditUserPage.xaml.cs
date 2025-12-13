using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.ViewModels;

namespace RestaurantManagementGUI
{
    public partial class EditUserPage : ContentPage
    {
        public EditUserPage(UserModel user)
        {
            InitializeComponent();
            // Lấy ApiService từ DI container
            var apiService = IPlatformApplication.Current.Services.GetService<ApiService>();
            BindingContext = new EditUserViewModel(apiService, user);
        }
    }
}