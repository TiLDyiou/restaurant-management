using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Input;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class TablesViewModel : ObservableObject, IDisposable
    {
        private readonly ApiService _apiService;

        private ObservableCollection<Ban> _allTables;
        private ObservableCollection<Ban> _filteredTables;

        public ObservableCollection<Ban> FilteredTables
        {
            get => _filteredTables;
            private set => SetProperty(ref _filteredTables, value);
        }

        public event EventHandler DataUpdated;
        public event EventHandler<Ban> TableSelected;
        public ICommand FilterTablesCommand { get; }
        public ICommand SelectTableCommand { get; }

        public TablesViewModel(ApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _allTables = new ObservableCollection<Ban>();
            FilteredTables = new ObservableCollection<Ban>();

            FilterTablesCommand = new RelayCommand(param => FilterTables(param as string));
            SelectTableCommand = new RelayCommand(param => SelectTable(param as Ban));
        }

        public async Task LoadTablesAsync()
        {
            var tableList = await _apiService.GetTablesAsync();
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
        public void SubscribeToSocket()
        {
            _ = SocketListener.Instance.ConnectAsync();
            SocketListener.Instance.OnTableStatusChanged -= HandleTableUpdate;
            SocketListener.Instance.OnTableStatusChanged += HandleTableUpdate;
        }

        public void UnsubscribeFromSocket()
        {
            SocketListener.Instance.OnTableStatusChanged -= HandleTableUpdate;
        }

        public void Dispose()
        {
            UnsubscribeFromSocket();
        }

        private void HandleTableUpdate(string json)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<Ban>(json, options);

                if (data != null && !string.IsNullOrEmpty(data.MaBan))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Cập nhật danh sách hiển thị
                        var tableFiltered = FilteredTables.FirstOrDefault(t => t.MaBan.Equals(data.MaBan, StringComparison.OrdinalIgnoreCase));
                        if (tableFiltered != null)
                        {
                            // Nhờ ObservableObject trong Model, dòng này sẽ làm UI đổi màu
                            tableFiltered.TrangThai = data.TrangThai;
                        }

                        // Cập nhật danh sách gốc (để khi filter không bị sai)
                        var tableMaster = _allTables.FirstOrDefault(t => t.MaBan.Equals(data.MaBan, StringComparison.OrdinalIgnoreCase));
                        if (tableMaster != null) tableMaster.TrangThai = data.TrangThai;

                        // Bắn event để cập nhật thống kê (số bàn trống/bận)
                        DataUpdated?.Invoke(this, EventArgs.Empty);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi Socket Table: {ex.Message}");
            }
        }

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
                default: 
                    filteredList = new ObservableCollection<Ban>(_allTables); 
                    break;
            }
            FilteredTables = filteredList;
        }

        public void SelectTable(Ban selectedTable)
        {
            if (selectedTable != null) TableSelected?.Invoke(this, selectedTable);
        }

        class RelayCommand : ICommand
        {
            Action<object> _action;
            public RelayCommand(Action<object> action) => _action = action;
            public bool CanExecute(object parameter) => true;
            public void Execute(object parameter) => _action(parameter);
            public event EventHandler CanExecuteChanged;
        }
    }
}