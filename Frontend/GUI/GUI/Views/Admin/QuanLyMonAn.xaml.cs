using RestaurantManagementGUI.ViewModels;

namespace RestaurantManagementGUI.Views.Admin
{
    public partial class QuanLyMonAnPage : ContentPage
    {
        private readonly FoodViewModel _viewModel;

        public QuanLyMonAnPage(FoodViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadDishesAsync();
        }
    }
}