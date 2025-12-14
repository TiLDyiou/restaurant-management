using RestaurantManagementGUI.ViewModels;

namespace RestaurantManagementGUI.Views
{
    public partial class FoodMenuPage : ContentPage
    {
        private readonly MenuViewerViewModel _viewModel;

        public FoodMenuPage()
        {
            InitializeComponent();
            _viewModel = new MenuViewerViewModel();
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadMenuAsync();
        }
    }
}