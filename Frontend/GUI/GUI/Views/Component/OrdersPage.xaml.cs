using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.ViewModels;

namespace RestaurantManagementGUI
{
    public partial class OrdersPage : ContentPage
    {
        private readonly FoodMenuViewModel _viewModel;

        public OrdersPage(Ban table)
        {
            InitializeComponent();
            _viewModel = new FoodMenuViewModel();
            _viewModel.TenBan = table.TenBan;
            _viewModel.RealTableId = table.MaBan;
            BindingContext = _viewModel;
        }

        public OrdersPage() : this(new Ban { TenBan = "Tự do", MaBan = "" }) { }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }

        private async void OnBackButtonClicked(object sender, EventArgs e) => await Navigation.PopAsync();
    }
}