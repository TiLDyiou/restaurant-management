using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Helpers; // Nhớ using cái này để lấy ApiConfig

namespace RestaurantManagementGUI.ViewModels
{
    public partial class ChefOrdersViewModel : ObservableObject
    {
        private HubConnection? _hubConnection;
        private readonly HttpClient _httpClient;

        // SỬA LỖI 1: Dùng luôn BaseUrl từ ApiConfig để không bao giờ sai Port nữa
        // Cắt bỏ phần "api/" ở cuối vì Hub không nằm trong thư mục api
        private string HubUrl => ApiConfig.BaseUrl.TrimEnd('/') + "/../kitchenHub";

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
            // Cấu hình HttpClient bỏ qua SSL
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler) { BaseAddress = new Uri(ApiConfig.BaseUrl) };

            InitializeSignalR();
            LoadInitialOrders();
        }

        private async void InitializeSignalR()
        {
            // 1. CHẶN LẶP KẾT NỐI: Nếu đã kết nối rồi thì return ngay (Quan trọng cho Singleton)
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                return;

            // Chỉ khởi tạo nếu chưa có connection
            if (_hubConnection == null)
            {
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

                // 2. XỬ LÝ KHI NHẬN ĐƠN: Cập nhật + Hỏi người dùng + Chuyển trang
                _hubConnection.On<object>("ReceiveOrder", (data) =>
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        // Tải lại danh sách trước
                        await LoadInitialOrders();
                        NewOrderCount++;

                        // Hiện thông báo có 2 nút: "Xem ngay" (true) và "Đóng" (false)
                        bool viewNow = await Application.Current.MainPage.DisplayAlert(
                            "BẾP - CÓ ĐƠN MỚI",
                            $"🔔 Bạn vừa nhận được đơn mới! (Chờ xử lý: {NewOrderCount})",
                            "Xem ngay",
                            "Để sau");

                        if (viewNow)
                        {
                            NewOrderCount = 0;
                            try
                            {
                                // KIỂM TRA: Nếu đang dùng Shell thì đi đường Shell
                                if (Shell.Current != null)
                                {
                                    // Đảm bảo route này đã được đăng ký trong AppShell.xaml.cs
                                    await Shell.Current.GoToAsync("ChefOrdersPage");
                                    // Lưu ý: Nếu dùng đường dẫn tuyệt đối thì thêm // phía trước: "//ChefOrdersPage"
                                }
                                else
                                {
                                    // NẾU KHÔNG DÙNG SHELL (Trường hợp của bạn đang bị lỗi null)
                                    // Ta dùng Navigation truyền thống
                                    // 'this' ở đây chính là ChefOrdersViewModel hiện tại, truyền nó sang trang kia
                                    await Application.Current.MainPage.Navigation.PushAsync(new ChefOrdersPage(this));
                                }
                            }
                            catch (Exception ex)
                            {
                                // In lỗi ra để biết đường sửa nếu vẫn không chuyển được
                                await Application.Current.MainPage.DisplayAlert("Lỗi chuyển trang", ex.Message, "OK");
                            }
                        }
                    });
                });

                // Xử lý khi mất kết nối
                _hubConnection.Closed += async (error) =>
                {
                    IsConnected = false;
                    await MainThread.InvokeOnMainThreadAsync(() =>
                         Application.Current.MainPage.DisplayAlert("Mất kết nối", "SignalR bị ngắt, đang thử lại...", "OK"));
                };
            }

            // Bắt đầu kết nối
            try
            {
                if (_hubConnection.State == HubConnectionState.Disconnected)
                {
                    await _hubConnection.StartAsync();
                    await _hubConnection.InvokeAsync("JoinKitchenGroup");
                    IsConnected = true;
                    Console.WriteLine("SignalR Connected!");
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                // Chỉ log lỗi ra console để tránh spam thông báo khi mở app
                Console.WriteLine($"Lỗi kết nối SignalR: {ex.Message}");
            }
        }

        public async Task LoadInitialOrders()
        {
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                // Gọi API lấy danh sách mới nhất
                var orders = await _httpClient.GetFromJsonAsync<List<HoaDonModel>>("orders/get-all-orders-info", options);
                if (orders != null)
                {
                    // Lọc ra các đơn chưa xong
                    var pendingOrders = orders
                        .Where(x => x.TrangThai != "Đã hoàn thành" && x.TrangThai != "Đã thanh toán")
                        .OrderByDescending(x => x.NgayLap);

                    MainThread.BeginInvokeOnMainThread(() => {
                        // QUAN TRỌNG: Phải xóa sạch danh sách cũ trước khi thêm mới
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

        [RelayCommand]
        async Task CompleteDish(ChiTietHoaDonModel item)
        {
            if (item == null) return;

            // Tìm hóa đơn cha
            var parentOrder = ActiveOrders.FirstOrDefault(o => o.ChiTietHoaDons.Contains(item));

            // Gọi API (giả sử API chạy ok)
            var res = await _httpClient.PutAsJsonAsync(
                 $"orders/update-dishes-status?maHD={parentOrder?.MaHD}&maMA={item.MaMA}",
                 new UpdateOrderItemStatusDto { NewStatus = "Đã xong" });

            if (res.IsSuccessStatusCode)
            {
                // CHỈ CẦN DÒNG NÀY LÀ ĐỦ (Vì đã sửa Model ở Bước 1)
                item.TrangThai = "Đã xong";

                // Báo cho nút to check lại điều kiện
                CompleteOrderCommand.NotifyCanExecuteChanged();
            }
        }

        // --- Command 2: Xử lý xong cả bàn (Có điều kiện CanExecute) ---
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

        // Điều kiện để nút sáng lên
        private bool CanCompleteOrder(HoaDonModel order)
        {
            if (order == null || order.ChiTietHoaDons == null) return false;
            // Kiểm tra xem tất cả món đã xong chưa
            return order.ChiTietHoaDons.All(x => x.IsDone);
        }
    }
}