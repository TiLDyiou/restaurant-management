using RestaurantManagementGUI.ViewModels;

namespace RestaurantManagementGUI
{
    public partial class BillGenerationPage : ContentPage
    {
        public BillGenerationPage(BillGenerationViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}