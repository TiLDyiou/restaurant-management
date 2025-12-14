using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class TablesViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private List<Ban> _allTables = new();

        [ObservableProperty]
        private ObservableCollection<Ban> filteredTables = new();

        // --- PHẦN THÊM MỚI CHO THÔNG BÁO ---
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNewNotifications))]
        private int newNotificationCount = 0;
        public bool HasNewNotifications => NewNotificationCount > 0;

        [ObservableProperty]
        private ObservableCollection<string> notificationList = new();

        [ObservableProperty]
        private bool showNotificationPopup;
        // -----------------------------------

        public event EventHandler DataUpdated;

        public TablesViewModel()
        {
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
            _httpClient = new HttpClient(handler) { BaseAddress = new Uri(ApiConfig.BaseUrl) };
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task LoadTablesAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<Ban>>>(ApiConfig.Tables, _jsonOptions);
                if (response != null && response.Success)
                {
                    _allTables = response.Data ?? new List<Ban>();
                    FilterTables("Tất cả");
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        // --- SOCKET ---
        public void SubscribeSocket()
        {
            // Lắng nghe cập nhật bàn
            _ = SocketListener.Instance.ConnectAsync();
            SocketListener.Instance.OnTableStatusChanged -= HandleTableUpdate;
            SocketListener.Instance.OnTableStatusChanged += HandleTableUpdate;
            // Lắng nghe cập nhật thông báo từ bếp
            SocketListener.Instance.OnDishDone -= HandleDishDone;
            SocketListener.Instance.OnDishDone += HandleDishDone;
        }

        public void UnsubscribeSocket()
        {
            SocketListener.Instance.OnTableStatusChanged -= HandleTableUpdate;
            SocketListener.Instance.OnDishDone -= HandleDishDone;
        }

        private void HandleTableUpdate(string json)
        {
            try
            {
                var updatedBan = JsonSerializer.Deserialize<Ban>(json, _jsonOptions);
                if (updatedBan != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var table = _allTables.FirstOrDefault(t => t.MaBan == updatedBan.MaBan);
                        if (table != null)
                        {
                            table.TrangThai = updatedBan.TrangThai; // UI đổi màu
                            DataUpdated?.Invoke(this, EventArgs.Empty); // Cập nhật thống kê
                        }
                    });
                }
            }
            catch { }
        }
        // Xử lý khi Bếp báo xong món
        private void HandleDishDone(string message)
        {
            // message có thể là "Bàn B01: Gà rán đã xong"
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NewNotificationCount++;
                string time = DateTime.Now.ToString("HH:mm");
                NotificationList.Insert(0, $"{message} ({time})");
            });
        }
        [RelayCommand]
        public void FilterTables(string filterType)
        {
            if (_allTables == null) return;
            IEnumerable<Ban> result = _allTables;

            if (filterType == "Bàn trống")
                result = _allTables.Where(t => t.TrangThai == "Trống" || t.TrangThai == "Bàn trống");
            else if (filterType == "Bàn bận")
                result = _allTables.Where(t => t.TrangThai == "Có khách" || t.TrangThai == "Bàn bận");
            else if (filterType == "Bàn đã đặt")
                result = _allTables.Where(t => t.TrangThai == "Bàn đã đặt" || t.TrangThai == "Đã đặt");

            FilteredTables = new ObservableCollection<Ban>(result);
            DataUpdated?.Invoke(this, EventArgs.Empty);
        }

        // --- COMMANDS CHO THÔNG BÁO ---
        [RelayCommand]
        void ToggleNotifications()
        {
            ShowNotificationPopup = !ShowNotificationPopup;
            if (ShowNotificationPopup) 
                NewNotificationCount = 0;
        }

        [RelayCommand]
        void ClearAllNotifications()
        {
            NotificationList.Clear();
            ShowNotificationPopup = false;
        }
    }
}