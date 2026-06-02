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

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is BillGenerationViewModel vm)
            {
                await vm.LoadPendingBills();
            }
        }
    }
}