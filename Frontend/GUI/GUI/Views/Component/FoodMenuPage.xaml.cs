using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Helpers;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Linq;
using System;
using Microsoft.Maui.Controls;

namespace RestaurantManagementGUI.Views
{
    public partial class FoodMenuPage : ContentPage
    {
        private readonly HttpClient _httpClient;

        // Sử dụng ObservableCollection để UI tự cập nhật khi thêm/xóa
        public ObservableCollection<DishGroup> GroupedDishes { get; set; } = new();

        public FoodMenuPage()
        {
            InitializeComponent();

            // Khởi tạo HttpClient
            try
            {
#if DEBUG
                _httpClient = new HttpClient(GetInsecureHandler());
#else
                _httpClient = new HttpClient();
#endif
                // Đổi lại IP/Port cho đúng với server của bạn
                _httpClient.BaseAddress = new Uri("https://localhost:7004/");
            }
            catch (Exception ex)
            {
                DisplayAlert("Lỗi khởi tạo", ex.Message, "OK");
            }

            // Quan trọng: Gán BindingContext là chính trang này để XAML nhận diện được GroupedDishes
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Load lại dữ liệu mỗi khi trang hiện lên
            await LoadDishesAsync();
        }

        private async Task LoadDishesAsync()
        {
            if (_httpClient == null) return;

            try
            {
                // Gọi API lấy danh sách món ăn
                var dishes = await _httpClient.GetFromJsonAsync<List<Dish>>("/api/dishes/get-dishes");

                if (dishes != null && dishes.Any())
                {
                    // Group dữ liệu theo Loại (Category)
                    var groupedData = dishes
                        .GroupBy(d => d.Loai?.Trim().ToUpperInvariant() ?? "KHÁC") // Xử lý null safety
                        .Select(group => new DishGroup(
                            group.First().Loai?.Trim() ?? "KHÁC",
                            group.ToList()
                        ))
                        .OrderBy(g => g.Category)
                        .ToList();

                    // Cập nhật UI trên MainThread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        GroupedDishes.Clear();
                        foreach (var group in groupedData)
                        {
                            GroupedDishes.Add(group);
                        }
                    });
                }
                else
                {
                    await DisplayAlert("Thông báo", "Không tìm thấy món ăn nào.", "OK");
                }
            }
            catch (Exception ex)
            {
                // Debug.WriteLine(ex.Message); // Tốt nhất nên dùng Debug
                await DisplayAlert("Lỗi kết nối", $"Không thể tải thực đơn: {ex.Message}", "OK");
            }
        }

        // Helper bỏ qua SSL cho localhost (Chỉ dùng khi Debug)
        private HttpClientHandler GetInsecureHandler()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) =>
            {
                if (sender is HttpRequestMessage request)
                {
                    return request.RequestUri.IsLoopback ||
                           (DeviceInfo.Platform == DevicePlatform.Android && request.RequestUri.Host == "10.0.2.2");
                }
                return errors == System.Net.Security.SslPolicyErrors.None;
            };
            return handler;
        }
    }
}