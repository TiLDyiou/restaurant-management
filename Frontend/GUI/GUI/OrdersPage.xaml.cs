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
            _viewModel.RealTableId = ConvertToTableId(tenBan);
        }
        private string ConvertToTableId(string tenBan)
        {
            try
            {
                // Lấy phần số từ chuỗi (Ví dụ "Bàn 5" -> lấy số 5)
                string numberPart = System.Text.RegularExpressions.Regex.Match(tenBan, @"\d+").Value;

                if (int.TryParse(numberPart, out int number))
                {
                    // Format thành Bxx (Ví dụ số 5 -> "05", số 10 -> "10")
                    return $"B{number:D2}";
                }
            }
            catch { }

            return "B01"; // Fallback nếu lỗi
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
