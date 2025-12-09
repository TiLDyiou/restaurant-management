using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR.Client;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class ChefOrdersViewModel : ObservableObject
    {
        private HubConnection? _hubConnection;
        private readonly HttpClient _httpClient;

        [ObservableProperty]
        private ObservableCollection<HoaDonModel> activeOrders = new();

        [ObservableProperty]
        private bool isConnected;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNewOrders))]
        private int newOrderCount = 0;

        public bool HasNewOrders => NewOrderCount > 0;

        public ChefOrdersViewModel()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler) { BaseAddress = new Uri(ApiConfig.BaseUrl) };

            // Khởi tạo danh sách trước
            LoadInitialOrders();

            // Sau đó khởi tạo SignalR (chạy ngầm)
            _ = InitializeSignalR();
        }

        private async Task InitializeSignalR()
        {
            if (_hubConnection != null) return; // Chỉ khởi tạo 1 lần duy nhất

            try
            {
                // Logic URL chuẩn xác hơn
                string hubUrl = ApiConfig.BaseUrl.Replace("/api/", "/kitchenHub");

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, conf => {
                        conf.HttpMessageHandlerFactory = (x) => new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                        };
                    })
                    .WithAutomaticReconnect()
                    .Build();

                // --- ĐĂNG KÝ SỰ KIỆN (Chỉ làm 1 lần tại đây) ---

                _hubConnection.On<object>("ReceiveOrder", (data) =>
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        NewOrderCount++;
                        // Tải lại để đồng bộ dữ liệu mới nhất
                        await LoadInitialOrders();

                        // Logic hiển thị thông báo (như cũ)
                        bool viewNow = await Application.Current.MainPage.DisplayAlert(
                            "BẾP - CÓ ĐƠN MỚI",
                            $"🔔 Bạn có đơn mới! (Mới: {NewOrderCount})",
                            "Xem ngay", "Để sau");

                        if (viewNow)
                        {
                            NewOrderCount = 0;
                            // Reset count khi xem
                        }
                    });
                });

                _hubConnection.Closed += (error) =>
                {
                    IsConnected = false;
                    return Task.CompletedTask;
                };

                _hubConnection.Reconnected += (connectionId) =>
                {
                    IsConnected = true;
                    return Task.CompletedTask;
                };

                // Bắt đầu kết nối
                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("JoinKitchenGroup");
                IsConnected = true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                Console.WriteLine($"Lỗi SignalR: {ex.Message}");
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
                    // Lọc đơn chưa hoàn thành
                    var pendingOrders = orders
                        .Where(x => x.TrangThai != "Đã hoàn thành" && x.TrangThai != "Đã thanh toán")
                        .OrderByDescending(x => x.NgayLap) // Đơn mới nhất lên đầu
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
        void ResetNotificationCount() => NewOrderCount = 0;

        // --- XỬ LÝ MÓN ĂN (Đã sửa logic tìm cha) ---
        [RelayCommand]
        async Task CompleteDish(ChiTietHoaDonModel item)
        {
            if (item == null) return;

            // SỬA LỖI: Tìm cha dựa trên ID món ăn và Logic logic (thay vì Contains object)
            // Vì item này có thể là object cũ, ta phải tìm order nào chứa món có MaMA tương ứng
            var parentOrder = ActiveOrders.FirstOrDefault(o => o.ChiTietHoaDons.Any(ct => ct.MaMA == item.MaMA));

            if (parentOrder == null)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Không tìm thấy đơn hàng chứa món này (có thể danh sách đã thay đổi).", "OK");
                await LoadInitialOrders(); // Tải lại cho chắc
                return;
            }

            // Gọi API
            var res = await _httpClient.PutAsJsonAsync(
                 $"orders/update-dishes-status?maHD={parentOrder.MaHD}&maMA={item.MaMA}",
                 new UpdateOrderItemStatusDto { NewStatus = "Đã xong" });

            if (res.IsSuccessStatusCode)
            {
                // Cập nhật UI
                item.TrangThai = "Đã xong";

                // QUAN TRỌNG: Báo cho Command "Hoàn tất đơn" kiểm tra lại điều kiện
                // Vì CompleteOrderCommand phụ thuộc vào parentOrder, ta phải báo hiệu thay đổi
                CompleteOrderCommand.NotifyCanExecuteChanged();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Cập nhật thất bại", "OK");
            }
        }

        // --- XỬ LÝ CẢ ĐƠN ---
        [RelayCommand(CanExecute = nameof(CanCompleteOrder))]
        async Task CompleteOrder(HoaDonModel order)
        {
            if (order == null) return;

            var res = await _httpClient.PutAsJsonAsync(
                $"orders/update-all-dishes-in-{order.MaHD}-order-status",
                new UpdateOrderItemStatusDto { NewStatus = "Đã hoàn thành" });

            if (res.IsSuccessStatusCode)
            {
                ActiveOrders.Remove(order);
            }
        }

        // Điều kiện: Tất cả món phải là "Đã xong" thì mới cho bấm nút này
        private bool CanCompleteOrder(HoaDonModel order)
        {
            if (order == null || order.ChiTietHoaDons == null) return false;
            return order.ChiTietHoaDons.All(x => x.IsDone);
        }
    }
}