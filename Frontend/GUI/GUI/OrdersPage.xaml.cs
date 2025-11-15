// File: OrdersPage.xaml.cs
using Microsoft.Maui.Controls;

namespace RestaurantManagementGUI
{
    public partial class OrdersPage : ContentPage
    {
        private readonly FoodMenuViewModel _viewModel;

        // Constructor mặc định (dự phòng)
        public OrdersPage() : this("Đơn tự do")
        {
        }

        // Constructor nhận tên bàn
        public OrdersPage(string tenBan)
        {
            InitializeComponent();
            _viewModel = (FoodMenuViewModel)BindingContext;

            // Set tên bàn trực tiếp
            _viewModel.TenBan = $"{tenBan}";
        }

        // Được gọi khi page xuất hiện
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Initialize ViewModel để load dữ liệu
            await _viewModel.InitializeAsync();
        }

        // Handler cho nút Back
        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}