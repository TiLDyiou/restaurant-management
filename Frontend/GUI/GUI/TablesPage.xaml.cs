using RestaurantManagementGUI.ViewModels;
using System.Diagnostics;
namespace RestaurantManagementGUI; // Đảm bảo namespace này khớp với dự án của bạn

public partial class TablesPage : ContentPage
{
    private readonly TablesViewModel _viewModel;
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

#if WINDOWS
    var window = App.Current?.Windows[0];
    if (window != null)
    {
        window.Title = "";
    }
#endif
    }

    // 1. Yêu cầu Dependency Injection "tiêm" ViewModel vào constructor
    public TablesPage()
    {
        InitializeComponent();

        // 2. Gán ViewModel đã được tiêm vào BindingContext
        _viewModel = new TablesViewModel();
        BindingContext = _viewModel;
    }

    // 3. Gọi LoadTablesAsync mỗi khi trang xuất hiện
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Gọi hàm tải dữ liệu từ ViewModel
        // ViewModel sẽ tự xử lý lỗi (nếu có)
        try
        {
            await _viewModel.LoadTablesAsync();
        }
        catch (Exception ex)
        {
            // Lỗi này xảy ra nếu API thất bại (ví dụ: backend chưa chạy)
            Debug.WriteLine($"Không thể tải dữ liệu bàn: {ex.Message}");

            // Dùng "DisplayAlert" (của chính Page) thay vì "Shell.Current.DisplayAlert"
            // Cách này luôn an toàn.
            await DisplayAlert("Lỗi", "Không thể tải sơ đồ bàn ăn. Vui lòng thử lại.", "OK");
        }
    }
}