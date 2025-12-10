using RestaurantManagementGUI.ViewModels;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Collections.Specialized; // Cần cho NotifyCollectionChanged

namespace RestaurantManagementGUI
{
    public partial class TablesPage : ContentPage
    {
        private readonly TablesViewModel _viewModel;
        private readonly ApiService _apiService;
        private Ban _selectedTable;

        public TablesPage(TablesViewModel viewModel, ApiService apiService)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _apiService = apiService;
            BindingContext = _viewModel;
            SetupEvents();
        }

        public TablesPage()
        {
            InitializeComponent();
            _apiService = new ApiService();
            _viewModel = new TablesViewModel(_apiService);
            BindingContext = _viewModel;
            SetupEvents();
        }

        private void SetupEvents()
        {
            _viewModel.TableSelected += OnTableSelected;
            _viewModel.DataUpdated += (s, e) => FlyoutMenu.UpdateStatistics(_viewModel.FilteredTables);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadTablesAsync();
            _viewModel.SubscribeToSocket(); // Kích hoạt Socket
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.UnsubscribeFromSocket();
        }

        private void OnFilteredTablesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Dùng 'FilteredTables'
            FlyoutMenu.UpdateStatistics(_viewModel.FilteredTables);
        }

        private async void OnHamburgerTapped(object sender, EventArgs e)
        {
            FlyoutMenu.SelectedTable = null;
            await FlyoutMenu.OpenAsync();
        }

        private async void OnTableSelected(object sender, Ban selectedTable)
        {
            if (selectedTable == null) return;
            _selectedTable = selectedTable;
            FlyoutMenu.SelectedTable = selectedTable;
            await FlyoutMenu.OpenAsync();
        }

        private async void OnFlyoutChangeStatusRequested(object sender, Ban table)
        {
            await FlyoutMenu.CloseAsync();

            var possibleStatuses = new[] { "Bàn bận", "Bàn trống", "Bàn đã đặt" };
            var otherStatuses = possibleStatuses.Where(s => s != table.TrangThai).ToArray();

            string statusAction = await DisplayActionSheet(
                $"Chọn trạng thái mới cho {table.TenBan}", "Hủy", null, otherStatuses
            );

            if (statusAction == null || statusAction == "Hủy")
                return;

            try
            {
                bool success = await _apiService.UpdateTableStatusAsync(table.MaBan, statusAction);

                if (success)
                {
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

        private async void OnFlyoutViewAddOrderRequested(object sender, Ban table)
        {
            await FlyoutMenu.CloseAsync();
            await Navigation.PushAsync(new OrdersPage(table.TenBan));
        }

        private async void OnFlyoutPaymentRequested(object sender, Ban table)
        {
            await FlyoutMenu.CloseAsync();
            await Navigation.PushAsync(new BillGenerationPage());
        }

        private async void OnFlyoutRefreshRequested(object sender, EventArgs e)
        {
            try
            {
                await _viewModel.LoadTablesAsync();
                await FlyoutMenu.CloseAsync();
                await DisplayAlert("Thành công", "Đã làm mới danh sách bàn", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi làm mới: {ex.Message}");
                await DisplayAlert("Lỗi", $"Không thể làm mới: {ex.Message}", "OK");
            }
        }

        private void OnFlyoutFilterChanged(object sender, string filterType)
        {
            // Dùng 'FilterTablesCommand' (được tạo bởi [RelayCommand])
            _viewModel.FilterTablesCommand.Execute(filterType);
        }
    }
}