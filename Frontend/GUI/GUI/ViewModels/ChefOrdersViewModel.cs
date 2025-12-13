using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using System.Text.Json;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class ChefOrdersViewModel : BaseViewModel, IDisposable
    {
        private readonly ApiService _apiService;

        // Danh sách đơn hàng đang nấu
        public ObservableCollection<HoaDonModel> ActiveOrders { get; } = new();

        // Danh sách thông báo
        public ObservableCollection<string> NotificationList { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNewOrders))]
        private int _newOrderCount;

        public bool HasNewOrders => NewOrderCount > 0;

        [ObservableProperty]
        private bool _showNotificationPopup;

        public ChefOrdersViewModel(ApiService apiService)
        {
            _apiService = apiService;

            // Tải dữ liệu ban đầu
            LoadInitialOrdersCommand.Execute(null);

            // Kết nối Socket
            InitializeSocket();
        }

        private void InitializeSocket()
        {
            _ = SocketListener.Instance.ConnectAsync();
            SocketListener.Instance.OnNewOrderReceived -= HandleNewOrder;
            SocketListener.Instance.OnNewOrderReceived += HandleNewOrder;
        }

        [RelayCommand]
        public async Task LoadInitialOrdersAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            var response = await _apiService.GetAsync<List<HoaDonModel>>(ApiConfig.Orders);

            if (response.Success && response.Data != null)
            {
                ActiveOrders.Clear();
                // Chỉ lấy đơn chưa hoàn thành và chưa thanh toán
                var pending = response.Data
                    .Where(x => x.TrangThai != "Đã hoàn thành" && x.TrangThai != "Đã thanh toán")
                    .OrderByDescending(x => x.NgayLap);

                foreach (var order in pending) ActiveOrders.Add(order);
            }
            IsBusy = false;
        }

        private void HandleNewOrder(string jsonPayload)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var newOrder = JsonSerializer.Deserialize<HoaDonModel>(jsonPayload, options);

                if (newOrder != null)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        // Thêm vào đầu danh sách
                        ActiveOrders.Insert(0, newOrder);

                        // Cập nhật thông báo
                        NewOrderCount++;
                        string time = DateTime.Now.ToString("HH:mm");
                        NotificationList.Insert(0, $"Bàn {newOrder.TableName} vừa gọi món ({time})");

                        // Giới hạn 10 tin
                        if (NotificationList.Count > 10) NotificationList.RemoveAt(NotificationList.Count - 1);

                        // Rung hoặc phát tiếng chuông (nếu cần)
                        try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500)); } catch { }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CHEF SOCKET ERROR] {ex.Message}");
            }
        }

        [RelayCommand]
        public void ToggleNotifications()
        {
            ShowNotificationPopup = !ShowNotificationPopup;
            if (ShowNotificationPopup) NewOrderCount = 0; // Reset đếm khi mở xem
        }

        [RelayCommand]
        public void ClearAllNotifications()
        {
            NotificationList.Clear();
            ShowNotificationPopup = false;
        }

        // Cập nhật 1 món xong
        [RelayCommand]
        public async Task CompleteDishAsync(ChiTietHoaDonModel item)
        {
            if (item == null || item.IsDone) return;

            // Tìm đơn cha chứa món này để lấy MaHD
            var parentOrder = ActiveOrders.FirstOrDefault(o => o.ChiTietHoaDons.Contains(item));
            if (parentOrder == null) return;

            // Gọi API cập nhật món (dùng URL helper từ ApiConfig)
            var url = ApiConfig.UpdateOrderItemStatus(parentOrder.MaHD, item.MaMA);
            var response = await _apiService.PutAsync<object>(url, new UpdateOrderItemStatusDto { NewStatus = "Đã xong" });

            if (response.Success)
            {
                item.TrangThai = "Đã xong"; // UI tự cập nhật nhờ ObservableObject

                // Kiểm tra xem xong cả bàn chưa để enable nút "Xong cả bàn"
                // (Logic này UI tự lo nhờ Binding, hoặc gọi NotifyCanExecuteChanged nếu dùng RelayCommand CanExecute)
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", response.Message, "OK");
            }
        }

        // Xong cả bàn (khi tất cả món đã xong)
        [RelayCommand]
        public async Task CompleteOrderAsync(HoaDonModel order)
        {
            if (order == null) return;

            // Kiểm tra client-side cho chắc
            if (order.ChiTietHoaDons.Any(x => !x.IsDone))
            {
                bool confirm = await Application.Current.MainPage.DisplayAlert("Cảnh báo",
                    "Vẫn còn món chưa nấu xong. Bạn có chắc chắn muốn hoàn thành đơn này?", "Có", "Không");
                if (!confirm) return;
            }

            var url = ApiConfig.UpdateOrderStatus(order.MaHD);
            var response = await _apiService.PutAsync<object>(url, new UpdateOrderStatusDto { NewStatus = "Đã hoàn thành" });

            if (response.Success)
            {
                ActiveOrders.Remove(order);
                await Application.Current.MainPage.DisplayAlert("Thành công", $"Đã hoàn thành đơn bàn {order.TableName}", "OK");
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", response.Message, "OK");
            }
        }

        public void Dispose()
        {
            SocketListener.Instance.OnNewOrderReceived -= HandleNewOrder;
        }
    }
}