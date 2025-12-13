using RestaurantManagementGUI.ViewModels;

namespace RestaurantManagementGUI
{
    public partial class OrdersPage : ContentPage
    {
        private readonly OrderViewModel _viewModel;

        public OrdersPage() : this("Đơn tự do") { }

        public OrdersPage(string tenBan)
        {
            InitializeComponent();

            // Lấy ViewModel từ DI
            _viewModel = IPlatformApplication.Current.Services.GetService<OrderViewModel>();
            BindingContext = _viewModel;

            if (_viewModel != null)
            {
                _viewModel.TableName = tenBan;
                _viewModel.TableId = ConvertToTableId(tenBan);
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_viewModel != null)
            {
                await _viewModel.LoadMenuCommand.ExecuteAsync(null);
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private string ConvertToTableId(string tenBan)
        {
            try
            {
                // Tách số từ chuỗi "Bàn 5" -> "B05"
                string numberPart = System.Text.RegularExpressions.Regex.Match(tenBan, @"\d+").Value;
                if (int.TryParse(numberPart, out int number))
                {
                    return $"B{number:D2}"; // B01, B02, ..., B10
                }
            }
            catch { }
            return "B01"; // Fallback
        }
    }
}