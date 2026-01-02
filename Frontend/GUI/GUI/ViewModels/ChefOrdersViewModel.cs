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
        private readonly JsonSerializerOptions _jsonOptions;

        [ObservableProperty]
        private ObservableCollection<HoaDonDto> activeOrders = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNewOrders))]
        private int newOrderCount = 0;
        public bool HasNewOrders => NewOrderCount > 0;

        [ObservableProperty]
        private ObservableCollection<string> notificationList = new();

        [ObservableProperty]
        private bool showNotificationPopup;

        [ObservableProperty]
        private bool isBusy;

        public ChefOrdersViewModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            InitializeSocket();
            _ = LoadInitialOrders();
            _ = LoadNotificationsAsync();
        }

        private void InitializeSocket()
        {
            Task.Run(async () => await TCPSocketClient.Instance.ConnectAsync());

            TCPSocketClient.Instance.OnNewOrderReceived -= HandleNewOrder;
            TCPSocketClient.Instance.OnNewOrderReceived += HandleNewOrder;
        }

        public async Task LoadInitialOrders()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<HoaDonDto>>>(ApiConfig.Orders, _jsonOptions);

                if (response != null && response.Success && response.Data != null)
                {
                    var pendingOrders = response.Data
                        .Where(x => x.TrangThai != "Đã thanh toán" && x.TrangThai != "Đã hủy" && x.TrangThai != "Đã hoàn thành")
                        .OrderByDescending(x => x.NgayLap)
                        .ToList();

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ActiveOrders.Clear();
                        foreach (var order in pendingOrders) ActiveOrders.Add(order);
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chef Load Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void HandleNewOrder(string msgContent)
        {
            string maHD = msgContent.Trim();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var url = ApiConfig.OrderById(maHD);
                    var res = await _httpClient.GetFromJsonAsync<ApiResponse<HoaDonDto>>(url, _jsonOptions);

                    if (res != null && res.Success && res.Data != null)
                    {
                        if (!ActiveOrders.Any(o => o.MaHD == res.Data.MaHD))
                        {
                            NewOrderCount++;
                            ActiveOrders.Insert(0, res.Data);

                            string time = DateTime.Now.ToString("HH:mm");
                            NotificationList.Insert(0, $"Bàn {res.Data.MaBan} lên đơn mới ({time})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling new order: {ex.Message}");
                }
            });
        }

        [RelayCommand]
        async Task CompleteDish(ChiTietHoaDonViewDto item)
        {
            if (item == null) return;

            var parentOrder = ActiveOrders.FirstOrDefault(o => o.ChiTietHoaDons.Contains(item));
            if (parentOrder == null) return;

            try
            {
                var url = ApiConfig.UpdateOrderItemStatus(parentOrder.MaHD, item.MaMA);

                var payload = new { NewStatus = "Đã xong" };

                var res = await _httpClient.PutAsJsonAsync(url, payload);

                if (res.IsSuccessStatusCode)
                {
                    item.TrangThai = "Đã xong";
                    CompleteOrderCommand.NotifyCanExecuteChanged();
                }
                else
                {
                    var err = await res.Content.ReadAsStringAsync();
                    await Application.Current.MainPage.DisplayAlert("Lỗi Backend", err, "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi Kết Nối", ex.Message, "OK");
            }
        }

        [RelayCommand(CanExecute = nameof(CanCompleteOrder))]
        async Task CompleteOrder(HoaDonDto order)
        {
            if (order == null || IsBusy) return;

            IsBusy = true;
            try
            {
                var url = ApiConfig.UpdateOrderStatus(order.MaHD);
                var res = await _httpClient.PutAsJsonAsync(url, new { NewStatus = "Đã hoàn thành" });

                if (res.IsSuccessStatusCode)
                {
                    ActiveOrders.Remove(order);
                    await Application.Current.MainPage.DisplayAlert("Bếp", $"Đã hoàn thành đơn {order.MaHD}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanCompleteOrder(HoaDonDto order)
        {
            if (order == null || order.ChiTietHoaDons == null) return false;
            return order.ChiTietHoaDons.All(x => x.IsDone);
        }

        [RelayCommand]
        void ToggleNotifications()
        {
            ShowNotificationPopup = !ShowNotificationPopup;
            if (ShowNotificationPopup)
                NewOrderCount = 0;
        }

        public async Task LoadNotificationsAsync()
        {
            try
            {
                string url = $"{ApiConfig.Notifications}?loai=BEP";

                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ThongBaoDto>>>(url, _jsonOptions);

                if (response != null && response.Success && response.Data != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        NotificationList.Clear();
                        foreach (var item in response.Data)
                        {
                            string time = item.ThoiGian.ToString("HH:mm");
                            NotificationList.Add($"{item.NoiDung} ({time})");
                        }
                        NewOrderCount = NotificationList.Count;
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chef Load Noti Error: {ex.Message}");
            }
        }

        [RelayCommand]
        async Task ClearAllNotifications()
        {
            NotificationList.Clear();
            ShowNotificationPopup = false;
            NewOrderCount = 0;

            try
            {
                string url = $"{ApiConfig.Notifications}?loai=BEP";
                await _httpClient.DeleteAsync(url);
            }
            catch { }
        }
    }
}