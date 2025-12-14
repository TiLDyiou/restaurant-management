using RestaurantManagementGUI.ViewModels;

namespace RestaurantManagementGUI
{
    public partial class ChefOrdersPage : ContentPage
    {
        public ChefOrdersPage()
        {
            InitializeComponent();
            BindingContext = new ChefOrdersViewModel();
        }
    }
}