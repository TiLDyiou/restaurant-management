using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.ViewModels;

namespace RestaurantManagementGUI
{
    public partial class OrdersPage : ContentPage
    {
        private readonly FoodMenuViewModel _viewModel;
        public OrdersPage(FoodMenuViewModel viewModel, Ban table)
        {
            InitializeComponent();

            _viewModel = viewModel;
            BindingContext = _viewModel;
            if (table != null)
            {
                _viewModel.TenBan = table.TenBan;
                _viewModel.RealTableId = table.MaBan;
            }
        }

        public OrdersPage(FoodMenuViewModel viewModel)
            : this(viewModel, new Ban { TenBan = "Đơn tự do", MaBan = "" })
        {
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }

        private async void OnBackButtonClicked(object sender, EventArgs e) => await Navigation.PopAsync();
    }
}