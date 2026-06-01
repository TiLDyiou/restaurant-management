using System;
using System.Net.Http;
using Microsoft.Maui.Controls;
using RestaurantManagementGUI.ViewModels;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;

namespace RestaurantManagementGUI
{
    public partial class TablesPage : ContentPage
    {
        private readonly TablesViewModel _viewModel;

        public TablesPage(TablesViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
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
            await _viewModel.UpdateTableStatusCommand.ExecuteAsync(table);
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

        private async void OnFlyoutMergeRequested(object sender, Ban table)
        {
            await FlyoutMenu.CloseAsync();
            await _viewModel.MergeTablesCommand.ExecuteAsync(table);
        }

        private async void OnFlyoutSplitRequested(object sender, Ban table)
        {
            await FlyoutMenu.CloseAsync();
            await _viewModel.SplitTablesCommand.ExecuteAsync(table);
        }

        private async void OnFlyoutTransferRequested(object sender, Ban table)
        {
            await FlyoutMenu.CloseAsync();
            await _viewModel.TransferOrderCommand.ExecuteAsync(table);
        }

        private void OnFlyoutHistoryRequested(object sender, Ban table)
        {
            _viewModel.ShowTableHistoryCommand.Execute(table);
        }

        private async void OnFlyoutDeleteRequested(object sender, Ban table)
        {
            await FlyoutMenu.CloseAsync();
            await _viewModel.DeleteTableCommand.ExecuteAsync(table);
        }
    }
}