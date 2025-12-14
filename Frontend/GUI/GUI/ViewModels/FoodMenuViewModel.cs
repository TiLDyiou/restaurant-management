using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;
using System.Globalization;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class FoodMenuViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private ObservableCollection<CartItemModel> cartItems = new();

        // Danh sách gốc (không bao giờ thay đổi sau khi load)
        private List<FoodModel> _originalList = new();

        [ObservableProperty]
        private ObservableCollection<FoodModel> displayedFoodItems = new();

        [ObservableProperty]
        private ObservableCollection<string> categories = new();

        // --- FIX TÌM KIẾM ---
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterFoods(); // Tự động lọc khi gõ chữ
                }
            }
        }

        [ObservableProperty]
        private string selectedCategory = "Tất cả";

        [ObservableProperty]
        private string tenBan = "Đơn tự do";

        public string RealTableId { get; set; } = "B01";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Total))]
        private decimal subtotal;

        public decimal Total => Subtotal;

        public FoodMenuViewModel()
        {
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
            _httpClient = new HttpClient(handler) { BaseAddress = new Uri(ApiConfig.BaseUrl) };
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task InitializeAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<FoodModel>>>(ApiConfig.Dishes, _jsonOptions);

                if (response != null && response.Success && response.Data != null)
                {
                    // Lưu vào list gốc
                    _originalList = response.Data;

                    // Lấy danh sách Category (Chuẩn hóa)
                    Categories.Clear();
                    Categories.Add("Tất cả");

                    var cats = _originalList
                        .Select(x => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(x.Category?.Trim().ToLower() ?? "Khác"))
                        .Distinct()
                        .OrderBy(c => c);

                    foreach (var c in cats) Categories.Add(c);

                    FilterFoods();
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // --- LOGIC LỌC TỔNG HỢP (Category + Search) ---
        private void FilterFoods()
        {
            var query = _originalList.AsEnumerable();

            // 1. Lọc theo Category
            if (SelectedCategory != "Tất cả")
            {
                query = query.Where(f =>
                    (f.Category?.Trim().ToLower() ?? "khác") == SelectedCategory.ToLower());
            }

            // 2. Lọc theo Search Text (Tiếng Việt không dấu càng tốt, ở đây dùng Contains cơ bản)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string keyword = SearchText.ToLower();
                query = query.Where(f => f.Name.ToLower().Contains(keyword));
            }

            DisplayedFoodItems = new ObservableCollection<FoodModel>(query);
        }

        [RelayCommand]
        void Filter(string category)
        {
            SelectedCategory = category;
            FilterFoods();
        }

        [RelayCommand]
        void AddToCart(FoodModel food)
        {
            if (food == null) return;
            var existingItem = CartItems.FirstOrDefault(c => c.FoodItem.Id == food.Id);
            if (existingItem == null) CartItems.Add(new CartItemModel { FoodItem = food, Quantity = 1 });
            else existingItem.Quantity++;
            UpdateTotals();
        }

        [RelayCommand]
        void IncreaseQuantity(CartItemModel item)
        {
            if (item == null) return;
            item.Quantity++;
            UpdateTotals();
        }

        [RelayCommand]
        void DecreaseQuantity(CartItemModel item)
        {
            if (item == null) return;
            if (item.Quantity > 1) item.Quantity--;
            else CartItems.Remove(item);
            UpdateTotals();
        }

        [RelayCommand]
        void CancelOrder()
        {
            CartItems.Clear();
            UpdateTotals();
        }

        private void UpdateTotals()
        {
            Subtotal = CartItems.Sum(c => c.TotalPrice);
            OnPropertyChanged(nameof(Total));
        }

        [RelayCommand]
        async Task Checkout()
        {
            if (CartItems.Count == 0) return;

            bool confirm = await Application.Current.MainPage.DisplayAlert("Xác nhận", "Gửi đơn xuống bếp?", "Gửi ngay", "Hủy");
            if (!confirm) return;

            string maNV = await SecureStorage.Default.GetAsync("user_manv");

            var orderDto = new CreateHoaDonDto
            {
                MaBan = RealTableId,
                MaNV = maNV,
                ChiTietHoaDons = CartItems.Select(c => new ChiTietHoaDonDto
                {
                    MaMA = c.FoodItem.Id,
                    SoLuong = c.Quantity
                }).ToList()
            };

            try
            {
                IsLoading = true;
                var response = await _httpClient.PostAsJsonAsync(ApiConfig.Orders, orderDto);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);

                if (response.IsSuccessStatusCode && result != null && result.Success)
                {
                    await Application.Current.MainPage.DisplayAlert("Thành công", "Đã gửi đơn!", "OK");
                    CartItems.Clear();
                    UpdateTotals();
                    await Application.Current.MainPage.Navigation.PopAsync();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Lỗi", result?.Message ?? "Gửi đơn thất bại", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi hệ thống", ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}