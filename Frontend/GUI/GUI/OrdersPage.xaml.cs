// File: OrdersPage.xaml.cs
namespace RestaurantManagementGUI;

public partial class OrdersPage : ContentPage
{
    private FoodMenuViewModel _viewModel;

    // Hàm 1 (cho DashboardPage gọi)
    public OrdersPage()
    {
        InitializeComponent();
        _viewModel = new FoodMenuViewModel();
        BindingContext = _viewModel;
    }

    // Hàm 2 (cho TablesPage gọi)
    public OrdersPage(string tenBan)
    {
        InitializeComponent();
        _viewModel = new FoodMenuViewModel();
        _viewModel.TenBan = $"Số bàn: {tenBan}";
        BindingContext = _viewModel;
    }

    // Tải API mỗi khi trang được hiển thị
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel != null)
        {
            await _viewModel.InitializeAsync();
        }
    }

    // THÊM HÀM NÀY ĐỂ FIX LỖI XC0002 (Nút quay lại)
    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        // Dùng PopAsync vì bạn đang dùng NavigationPage (code cũ)
        await Navigation.PopAsync();
    }
}