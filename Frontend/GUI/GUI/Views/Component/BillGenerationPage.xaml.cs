using RestaurantManagementGUI.ViewModels;

namespace RestaurantManagementGUI
{
    public partial class BillGenerationPage : ContentPage
    {
        public BillGenerationPage()
        {
            InitializeComponent();
            // Lấy ViewModel từ DI
            BindingContext = IPlatformApplication.Current.Services.GetService<BillGenerationViewModel>();
        }
    }
}