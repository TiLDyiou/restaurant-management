using RestaurantManagementGUI.ViewModels;

namespace RestaurantManagementGUI
{
    public partial class ChefOrdersPage : ContentPage
    {
        // 1. KHAI BÁO BIẾN _viewModel TẠI ĐÂY (Để sửa lỗi gạch đỏ _viewModel)
        private readonly ChefOrdersViewModel _viewModel;

        // Constructor dùng cho Dependency Injection (Khuyên dùng)
        public ChefOrdersPage(ChefOrdersViewModel vm)
        {
            InitializeComponent();
            _viewModel = vm;
            BindingContext = _viewModel;
        }

        // Constructor rỗng (Dự phòng)
        public ChefOrdersPage()
        {
            InitializeComponent();
            // Lấy ViewModel từ Service
            var vm = IPlatformApplication.Current.Services.GetService<ChefOrdersViewModel>();
            _viewModel = vm;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Gọi hàm tải dữ liệu
            if (_viewModel != null)
            {
                // Nếu dòng này báo đỏ, hãy làm BƯỚC 2 bên dưới
                await _viewModel.LoadInitialOrders();
            }
        }
    }
}