using RestaurantManagementGUI.ViewModels;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace RestaurantManagementGUI;

public partial class TablesPage : ContentPage
{
    private readonly TablesViewModel _viewModel;
    private ObservableCollection<Ban> _allTables; // Lưu toàn bộ danh sách để lọc
    private Ban _selectedTable; // Lưu bàn đang chọn

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

    public TablesPage()
    {
        InitializeComponent();
        _viewModel = new TablesViewModel();

        // KẾT NỐI EVENT: Khi ViewModel trigger SelectTable
        _viewModel.TableSelected += OnTableSelected;

        BindingContext = _viewModel;
        _allTables = new ObservableCollection<Ban>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _viewModel.LoadTablesAsync();

            // Lưu danh sách gốc để phục vụ tính năng lọc
            _allTables = new ObservableCollection<Ban>(_viewModel.Tables);

            // Cập nhật thống kê trong Flyout Menu
            FlyoutMenu.UpdateStatistics(_viewModel.Tables);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Không thể tải dữ liệu bàn: {ex.Message}");
            await DisplayAlert("Lỗi", "Không thể tải sơ đồ bàn ăn. Vui lòng thử lại.", "OK");
        }
    }

    // ===== EVENT HANDLERS =====

    /// <summary>
    /// Xử lý khi nhấn nút Hamburger - Mở Flyout Menu (chế độ tổng quan)
    /// </summary>
    private async void OnHamburgerTapped(object sender, EventArgs e)
    {
        // Xóa thông tin bàn đã chọn trước đó
        FlyoutMenu.SelectedTable = null;

        await FlyoutMenu.OpenAsync();
    }

    /// <summary>
    /// Xử lý khi chọn bàn từ CollectionView (trigger từ ViewModel)
    /// </summary>
    private async void OnTableSelected(object sender, Ban selectedTable)
    {
        if (selectedTable == null) return;

        _selectedTable = selectedTable;

        // Cập nhật thông tin bàn trong Flyout
        FlyoutMenu.SelectedTable = selectedTable;

        // Mở Flyout với thông tin bàn đã chọn
        await FlyoutMenu.OpenAsync();
    }

    // ===== FLYOUT MENU EVENT HANDLERS =====

    /// <summary>
    /// 1. Đổi trạng thái bàn
    /// </summary>
    private async void OnFlyoutChangeStatusRequested(object sender, Ban table)
    {
        // Đóng Flyout trước khi hiện ActionSheet
        await FlyoutMenu.CloseAsync();

        var possibleStatuses = new[] { "Bàn bận", "Bàn trống", "Bàn đã đặt" };
        var otherStatuses = possibleStatuses.Where(s => s != table.TrangThai).ToArray();

        string statusAction = await DisplayActionSheet(
            $"Chọn trạng thái mới cho {table.TenBan}",
            "Hủy",
            null,
            otherStatuses
        );

        if (statusAction == null || statusAction == "Hủy")
            return;

        try
        {
            var apiService = new ApiService();
            bool success = await apiService.UpdateTableStatusAsync(table.MaBan, statusAction);

            if (success)
            {
                // Reload danh sách và cập nhật thống kê
                await _viewModel.LoadTablesAsync();
                _allTables = new ObservableCollection<Ban>(_viewModel.Tables);
                FlyoutMenu.UpdateStatistics(_viewModel.Tables);

                await DisplayAlert("Thành công", $"Đã đổi {table.TenBan} sang '{statusAction}'", "OK");
            }
            else
            {
                await DisplayAlert("Lỗi", "Không thể cập nhật trạng thái bàn.", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Lỗi cập nhật trạng thái: {ex.Message}");
            await DisplayAlert("Lỗi", $"Có lỗi xảy ra: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// 2. Xem/Thêm món
    /// </summary>
    private async void OnFlyoutViewAddOrderRequested(object sender, Ban table)
    {
        await FlyoutMenu.CloseAsync();

        // Truyền tên bàn vào OrdersPage
        var ordersPage = new OrdersPage(table.TenBan);
        await Navigation.PushAsync(ordersPage);
    }

    /// <summary>
    /// 3. Thanh toán
    /// </summary>
    private async void OnFlyoutPaymentRequested(object sender, Ban table)
    {
        await FlyoutMenu.CloseAsync();

        // Điều hướng đến BillGenerationPage
        await Navigation.PushAsync(new BillGenerationPage());
    }

    /// <summary>
    /// 4. Làm mới danh sách bàn
    /// </summary>
    private async void OnFlyoutRefreshRequested(object sender, EventArgs e)
    {
        try
        {
            await _viewModel.LoadTablesAsync();
            _allTables = new ObservableCollection<Ban>(_viewModel.Tables);
            FlyoutMenu.UpdateStatistics(_viewModel.Tables);

            // Đóng flyout và thông báo thành công
            await FlyoutMenu.CloseAsync();
            await DisplayAlert("Thành công", "Đã làm mới danh sách bàn", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Lỗi làm mới: {ex.Message}");
            await DisplayAlert("Lỗi", $"Không thể làm mới: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// 5. Lọc bàn theo trạng thái
    /// </summary>
    private void OnFlyoutFilterChanged(object sender, string filterType)
    {
        if (_allTables == null || _allTables.Count == 0)
            return;

        ObservableCollection<Ban> filteredTables;

        switch (filterType)
        {
            case "Bàn trống":
                filteredTables = new ObservableCollection<Ban>(
                    _allTables.Where(t => t.TrangThai == "Bàn trống"));
                break;

            case "Bàn bận":
                filteredTables = new ObservableCollection<Ban>(
                    _allTables.Where(t => t.TrangThai == "Bàn bận"));
                break;

            case "Bàn đã đặt":
                filteredTables = new ObservableCollection<Ban>(
                    _allTables.Where(t => t.TrangThai == "Bàn đã đặt"));
                break;

            case "Tất cả bàn":
            default:
                filteredTables = new ObservableCollection<Ban>(_allTables);
                break;
        }

        // Cập nhật CollectionView với danh sách đã lọc
        _viewModel.Tables.Clear();
        foreach (var table in filteredTables)
        {
            _viewModel.Tables.Add(table);
        }
    }
}