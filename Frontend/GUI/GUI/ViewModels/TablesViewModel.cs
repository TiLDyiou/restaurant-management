using CommunityToolkit.Mvvm.ComponentModel; // vẫn dùng ObservableObject cho INotifyPropertyChanged tiện lợi
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel; // cho MainThread (nếu bạn dùng MAUI)
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class TablesViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly TableHubService _tableHubService;

        // Danh sách gốc, luôn chứa TẤT CẢ các bàn
        private ObservableCollection<Ban> _allTables;

        // Explicit public property (thay vì [ObservableProperty])
        private ObservableCollection<Ban> _filteredTables;
        public event EventHandler DataUpdated;
        public ObservableCollection<Ban> FilteredTables
        {
            get => _filteredTables;
            private set
            {
                if (_filteredTables != value)
                {
                    _filteredTables = value;
                    OnPropertyChanged(nameof(FilteredTables));
                }
            }
        }

        // ICommand cho lọc (thay vì [RelayCommand])
        public ICommand FilterTablesCommand { get; private set; }
        public ICommand SelectTableCommand { get; private set; }

        public Page CurrentPage { get; set; }
        public event EventHandler<Ban> TableSelected;

        public TablesViewModel(ApiService apiService, TableHubService tableHubService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _tableHubService = tableHubService ?? throw new ArgumentNullException(nameof(tableHubService));

            _allTables = new ObservableCollection<Ban>();
            FilteredTables = new ObservableCollection<Ban>();

            // Khởi tạo command thủ công
            FilterTablesCommand = new RelayCommand(param => FilterTables(param as string));
            SelectTableCommand = new RelayCommand(param => SelectTable(param as Ban));

            // Đăng ký SignalR (hoặc event) nhận cập nhật
            _tableHubService.OnTableStatusChanged += HandleTableStatusUpdate;
        }

        // Load dữ liệu (async)
        public async Task LoadTablesAsync()
        {
            if (!_tableHubService.IsConnected)
            {
                await _tableHubService.InitAsync();
            }

            var tableList = await _apiService.GetTablesAsync();

            // Đảm bảo chạy trên UI thread (MAUI)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _allTables.Clear();
                FilteredTables.Clear();
                foreach (var table in tableList)
                {
                    _allTables.Add(table);
                    FilteredTables.Add(table);
                }
            });
        }

        private void HandleTableStatusUpdate(TableStatusUpdateDto update)
        {
            Debug.WriteLine($"[SignalR] Nhận cập nhật: Bàn {update.MaBan} -> {update.TrangThai}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var tableInMasterList = _allTables.FirstOrDefault(t => t.MaBan == update.MaBan);
                if (tableInMasterList != null)
                {
                    tableInMasterList.TrangThai = update.TrangThai;
                }

                var tableInFilteredList = FilteredTables.FirstOrDefault(t => t.MaBan == update.MaBan);
                if (tableInFilteredList != null)
                {
                    tableInFilteredList.TrangThai = update.TrangThai;
                }
                DataUpdated?.Invoke(this, EventArgs.Empty);
            });
        }

        // Hàm lọc (giữ private, command gọi vào)
        private void FilterTables(string filterType)
        {
            if (_allTables == null) return;

            ObservableCollection<Ban> filteredList;

            switch (filterType)
            {
                case "Bàn trống":
                    filteredList = new ObservableCollection<Ban>(_allTables.Where(t => t.TrangThai == "Bàn trống"));
                    break;
                case "Bàn bận":
                    filteredList = new ObservableCollection<Ban>(_allTables.Where(t => t.TrangThai == "Bàn bận"));
                    break;
                case "Bàn đã đặt":
                    filteredList = new ObservableCollection<Ban>(_allTables.Where(t => t.TrangThai == "Bàn đã đặt"));
                    break;
                case "Tất cả bàn":
                default:
                    filteredList = new ObservableCollection<Ban>(_allTables);
                    break;
            }

            // Cập nhật danh sách hiển thị
            FilteredTables = filteredList;
        }

        public void SelectTable(Ban selectedTable)
        {
            if (selectedTable == null) return;
            TableSelected?.Invoke(this, selectedTable);
        }

        public void SubscribeToSignalR()
        {
            // Đảm bảo không đăng ký trùng lặp
            _tableHubService.OnTableStatusChanged -= HandleTableStatusUpdate;
            _tableHubService.OnTableStatusChanged += HandleTableStatusUpdate;
            Debug.WriteLine("TablesViewModel: Đã ĐĂNG KÝ lắng nghe SignalR");
        }

        public void UnsubscribeFromSignalR()
        {
            _tableHubService.OnTableStatusChanged -= HandleTableStatusUpdate;
            Debug.WriteLine("TablesViewModel: Đã HỦY đăng ký SignalR");
        }

        // Simple RelayCommand implementation (không phụ thuộc toolkit source-gen)
        private class RelayCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Func<object?, bool>? _canExecute;

            public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

            public void Execute(object? parameter) => _execute(parameter);

            public event EventHandler? CanExecuteChanged;

            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
