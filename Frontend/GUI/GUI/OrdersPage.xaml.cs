using Microsoft.Maui.Controls;

namespace RestaurantManagementGUI
{
    public partial class OrdersPage : ContentPage
    {
        private readonly FoodMenuViewModel _viewModel;

        public OrdersPage() : this("Đơn tự do") { }

        public OrdersPage(string tenBan)
        {
            InitializeComponent();
            _viewModel = (FoodMenuViewModel)BindingContext;
            _viewModel.TenBan = tenBan;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
