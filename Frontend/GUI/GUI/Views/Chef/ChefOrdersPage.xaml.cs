namespace RestaurantManagementGUI
{
    public partial class ChefOrdersPage : ContentPage
    {
        // Constructor nhận ViewModel từ MauiProgram (Singleton)
        public ChefOrdersPage(ViewModels.ChefOrdersViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        // Nếu App gọi Constructor rỗng, tự lấy Service
        public ChefOrdersPage()
        {
            InitializeComponent();
            BindingContext = IPlatformApplication.Current.Services.GetService<ViewModels.ChefOrdersViewModel>();
        }
    }
}