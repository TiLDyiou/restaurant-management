using RestaurantManagementGUI.ViewModels;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using System.Net.Http.Json;
using RestaurantManagementGUI.Helpers;

namespace RestaurantManagementGUI
{
    public partial class TablesPage : ContentPage
    {
        private readonly TablesViewModel _viewModel;
        private readonly HttpClient _httpClient;

        public TablesPage(TablesViewModel viewModel, HttpClient httpClient)
        {
            InitializeComponent();

            _viewModel = viewModel;
            _httpClient = httpClient;

            BindingContext = _viewModel;
            _viewModel.DataUpdated += (s, e) => FlyoutMenu.UpdateStatistics(_viewModel.FilteredTables);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadTablesAsync();
            await _viewModel.LoadNotificationsAsync();
            _viewModel.SubscribeSocket();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.UnsubscribeSocket();
        }
        private async void OnHamburgerTapped(object sender, EventArgs e)
        {
            FlyoutMenu.SelectedTable = null;
            await FlyoutMenu.OpenAsync();
        }

        private async void OnTableItemTapped(object sender, TappedEventArgs e)
        {
            if (sender is Border border && border.BindingContext is Ban table)
            {
                FlyoutMenu.SelectedTable = table;
                await FlyoutMenu.OpenAsync();
            }
        }

        private async void OnFlyoutChangeStatusRequested(object sender, Ban table)
        {
            await FlyoutMenu.CloseAsync();
            var status = await DisplayActionSheet($"Trạng thái {table.TenBan}", "Hủy", null, "Trống", "Có khách", "Bàn đã đặt");
            if (string.IsNullOrEmpty(status) || status == "Hủy") return;

            try
            {
                var response = await _httpClient.PutAsJsonAsync(ApiConfig.UpdateTableStatus(table.MaBan), status);
                if (response.IsSuccessStatusCode)
                {
                    table.TrangThai = status;
                }
                else
                {
                    await DisplayAlert("Lỗi", "Cập nhật thất bại", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", ex.Message, "OK");
            }
        }

        private async void OnFlyoutViewAddOrderRequested(object sender, Ban table)
        {
            await FlyoutMenu.CloseAsync();
            var vm = Handler.MauiContext.Services.GetService<FoodMenuViewModel>();
            var ordersPage = new OrdersPage(vm, table);
            await Navigation.PushAsync(ordersPage);
        }

        private async void OnFlyoutPaymentRequested(object sender, Ban table)
        {
            var billPage = Handler.MauiContext.Services.GetService<BillGenerationPage>();
            await Navigation.PushAsync(billPage);
        }

        private async void OnFlyoutRefreshRequested(object sender, EventArgs e)
        {
            await _viewModel.LoadTablesAsync();
            await FlyoutMenu.CloseAsync();
        }

        private void OnFlyoutFilterChanged(object sender, string filterType)
        {
            _viewModel.FilterTables(filterType);
        }
    }
}