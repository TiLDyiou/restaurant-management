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
    public partial class ChefOrdersViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;

        [ObservableProperty]
        private ObservableCollection<HoaDonModel> activeOrders = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNewOrders))]
        private int newOrderCount = 0;
        public bool HasNewOrders => NewOrderCount > 0;

        [ObservableProperty]
        private ObservableCollection<string> notificationList = new();

        [ObservableProperty]
        private bool showNotificationPopup;

        public ChefOrdersViewModel()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (m, c, ch, e) => true;
            _httpClient = new HttpClient(handler) { BaseAddress = new Uri(ApiConfig.BaseUrl) };

            _ = LoadInitialOrders();

            // Đăng ký Socket
            InitializeSocket();
        }

        private void InitializeSocket()
        {
            _ = SocketListener.Instance.ConnectAsync();

            //Hủy đăng ký cũ trước khi đăng ký mới để tránh trùng lặp
            SocketListener.Instance.OnNewOrderReceived -= HandleNewOrder;
            SocketListener.Instance.OnNewOrderReceived += HandleNewOrder;
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
                        // Tăng số đếm -> Chấm đỏ tự hiện
                        NewOrderCount++;

                        // Thêm vào danh sách hiển thị
                        ActiveOrders.Insert(0, newOrder);

                        string time = DateTime.Now.ToString("HH:mm");
                        NotificationList.Insert(0, $"Bàn {newOrder.MaBan} vừa gửi đơn mới ({time})");

                        // Giới hạn chỉ giữ 10 thông báo gần nhất cho nhẹ
                        if (NotificationList.Count > 10) NotificationList.RemoveAt(NotificationList.Count - 1);

                        // Hiển thị Popup (DUY NHẤT TẠI ĐÂY)
                        await Application.Current.MainPage.DisplayAlert("👨‍🍳 BẾP", $"Có đơn mới bàn {newOrder.MaBan}", "OK");
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi Socket Bếp: {ex.Message}");
            }
        }

        public async Task LoadInitialOrders()
        {
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var orders = await _httpClient.GetFromJsonAsync<List<HoaDonModel>>(ApiConfig.GetAllOrders, options);

                if (orders != null)
                {
                    var pendingOrders = orders
                        .Where(x => x.TrangThai != "Đã hoàn thành" && x.TrangThai != "Đã thanh toán")
                        .OrderByDescending(x => x.NgayLap)
                        .ToList();

                    MainThread.BeginInvokeOnMainThread(() => {
                        ActiveOrders.Clear();
                        foreach (var order in pendingOrders)
                        {
                            ActiveOrders.Add(order);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi tải đơn: " + ex.Message);
            }
        }

        [RelayCommand]
        void ResetNotificationCount()
        {
            NewOrderCount = 0;
        }

        [RelayCommand]
        void ToggleNotifications()
        {
            // Bật/Tắt popup
            ShowNotificationPopup = !ShowNotificationPopup;

            // Nếu đang mở popup thì reset số đếm (mất chấm đỏ)
            if (ShowNotificationPopup)
            {
                NewOrderCount = 0;
            }
        }

        [RelayCommand]
        void ClearAllNotifications()
        {
            NotificationList.Clear();
            ShowNotificationPopup = false;
        }

        [RelayCommand]
        async Task CompleteDish(ChiTietHoaDonModel item)
        {
            if (item == null) return;
            var parentOrder = ActiveOrders.FirstOrDefault(o => o.ChiTietHoaDons.Any(ct => ct.MaMA == item.MaMA));
            if (parentOrder == null) return;

            var res = await _httpClient.PutAsJsonAsync(
                 $"orders/update-dishes-status?maHD={parentOrder.MaHD}&maMA={item.MaMA}",
                 new UpdateOrderItemStatusDto { NewStatus = "Đã xong" });

            if (res.IsSuccessStatusCode)
            {
                item.TrangThai = "Đã xong";
                CompleteOrderCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand(CanExecute = nameof(CanCompleteOrder))]
        async Task CompleteOrder(HoaDonModel order)
        {
            if (order == null) return;

            var res = await _httpClient.PutAsJsonAsync(
                $"orders/update-all-dishes-in-{order.MaHD}-order-status",
                new UpdateOrderStatusDto { NewStatus = "Đã hoàn thành" });

            if (res.IsSuccessStatusCode)
            {
                ActiveOrders.Remove(order);
            }
        }

        private bool CanCompleteOrder(HoaDonModel order)
        {
            if (order == null || order.ChiTietHoaDons == null) return false;
            return order.ChiTietHoaDons.All(x => x.IsDone);
        }
    }
}