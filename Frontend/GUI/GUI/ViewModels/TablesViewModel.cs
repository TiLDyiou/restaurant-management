using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class TablesViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<Ban> _tables;

        public Page CurrentPage { get; set; }

        public TablesViewModel()
        {
            _apiService = new ApiService();
            Tables = new ObservableCollection<Ban>();
        }

        [RelayCommand]
        public async Task LoadTablesAsync()
        {
            var tableList = await _apiService.GetTablesAsync();

            Tables.Clear();
            foreach (var table in tableList)
            {
                Tables.Add(table);
            }
        }

        // SelectTableCommand đã được đơn giản hóa
        // Logic xử lý sẽ do TablesPage.xaml.cs đảm nhận thông qua Flyout
        [RelayCommand]
        private void SelectTable(Ban selectedTable)
        {
            if (selectedTable == null) return;

            // Trigger event để TablesPage xử lý
            // (TablesPage sẽ mở Flyout khi bàn được chọn)
            TableSelected?.Invoke(this, selectedTable);
        }

        // Event để thông báo khi bàn được chọn
        public event EventHandler<Ban> TableSelected;
    }
}