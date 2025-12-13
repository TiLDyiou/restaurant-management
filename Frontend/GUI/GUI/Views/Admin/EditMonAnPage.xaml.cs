using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.ViewModels;

namespace RestaurantManagementGUI
{
    public partial class EditMonAnPage : ContentPage
    {
        public EditMonAnPage(FoodModel dish)
        {
            InitializeComponent();
            var apiService = IPlatformApplication.Current.Services.GetService<ApiService>();
            BindingContext = new EditDishViewModel(apiService, dish);
        }
    }
}