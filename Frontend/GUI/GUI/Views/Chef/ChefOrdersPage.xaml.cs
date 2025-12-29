using RestaurantManagementGUI.ViewModels;

namespace RestaurantManagementGUI
{
    public partial class ChefOrdersPage : ContentPage
    {
        public ChefOrdersPage(ChefOrdersViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}