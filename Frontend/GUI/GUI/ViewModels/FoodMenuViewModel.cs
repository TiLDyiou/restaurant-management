// File: FoodMenuViewModel.cs
// COPY TOÀN BỘ FILE NÀY - Thay thế hoàn toàn file cũ
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Helpers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Controls;
using System.Net.Http.Headers;
using Microsoft.Maui.Storage;

namespace RestaurantManagementGUI;

public partial class FoodMenuViewModel : ObservableObject
{
    private readonly HttpClient _httpClient;

    // Property hiển thị tên bàn
    private string _tenBan = "Đang tải bàn...";
    public string TenBan
    {
        get => _tenBan;
        set => SetProperty(ref _tenBan, value);
    }

    // Danh sách món ăn
    private List<FoodModel> _allFoodItems = new();
    public ObservableCollection<FoodModel> DisplayedFoodItems { get; set; } = new();
    public ObservableCollection<string> Categories { get; set; } = new();

    // Search và Category
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedCategory))]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchText))]
    private string _selectedCategory = "Tất cả";

    // Giỏ hàng
    public ObservableCollection<CartItemModel> CartItems { get; set; } = new();

    // Tổng tiền
    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _total;

    // Constructor
    public FoodMenuViewModel()
    {
        _httpClient = new HttpClient(GetInsecureHandler());
    }

    // ===== METHOD QUAN TRỌNG =====
    // Method này được gọi từ OrdersPage.xaml.cs khi page load
    public async Task InitializeAsync()
    {
        await LoadDataFromApiAsync();
    }

    // Load dữ liệu từ API
    private async Task LoadDataFromApiAsync()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync("auth_token");
            if (string.IsNullOrEmpty(token))
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Phiên đăng nhập hết hạn.", "OK");
                return;
            }
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            _allFoodItems = await _httpClient.GetFromJsonAsync<List<FoodModel>>(ApiConfig.GetFoodMenu);

            if (_allFoodItems == null)
            {
                _allFoodItems = new List<FoodModel>();
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Lỗi Tải Menu", $"Không thể tải API: {ex.Message}", "OK");
            _allFoodItems = new List<FoodModel>();
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Categories.Clear();
            Categories.Add("Tất cả");
            var categoriesFromApi = _allFoodItems.Select(item => item.Category).Distinct().ToList();
            foreach (var cat in categoriesFromApi)
            {
                if (!string.IsNullOrEmpty(cat))
                    Categories.Add(cat);
            }
            FilterItems();
        });
    }

    #region Filter & Cart Management

    partial void OnSearchTextChanged(string value) => FilterItems();
    partial void OnSelectedCategoryChanged(string value) => FilterItems();

    [RelayCommand]
    private void Filter(string category)
    {
        SelectedCategory = category ?? "Tất cả";
    }

    private void FilterItems()
    {
        DisplayedFoodItems.Clear();
        IEnumerable<FoodModel> filtered = _allFoodItems;

        if (SelectedCategory != "Tất cả")
        {
            filtered = filtered.Where(item => item.Category == SelectedCategory);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(item => item.Name.ToLower().Contains(SearchText.ToLower()));
        }

        foreach (var item in filtered)
        {
            DisplayedFoodItems.Add(item);
        }
    }

    [RelayCommand]
    private void AddToCart(FoodModel foodItem)
    {
        if (foodItem == null) return;

        var existingItem = CartItems.FirstOrDefault(item => item.FoodItem.Id == foodItem.Id);
        if (existingItem != null)
        {
            existingItem.Quantity++;
        }
        else
        {
            CartItems.Add(new CartItemModel { FoodItem = foodItem, Quantity = 1 });
        }
        UpdateCartSummary();
    }

    [RelayCommand]
    private void IncreaseQuantity(CartItemModel cartItem)
    {
        if (cartItem != null)
        {
            cartItem.Quantity++;
            UpdateCartSummary();
        }
    }

    [RelayCommand]
    private void DecreaseQuantity(CartItemModel cartItem)
    {
        if (cartItem != null)
        {
            cartItem.Quantity--;
            if (cartItem.Quantity == 0)
                CartItems.Remove(cartItem);
            UpdateCartSummary();
        }
    }

    #endregion

    #region GHI CHÚ MỚI

    // Command để thêm/sửa ghi chú
    [RelayCommand]
    private async Task AddNote(CartItemModel cartItem)
    {
        if (cartItem == null) return;

        string currentNote = cartItem.Note ?? string.Empty;
        string result = await Application.Current.MainPage.DisplayPromptAsync(
            title: "Ghi chú món ăn",
            message: $"Thêm ghi chú cho: {cartItem.FoodItem.Name}",
            placeholder: "VD: Ít cay, không hành, nhiều rau...",
            maxLength: 200,
            keyboard: Keyboard.Text,
            initialValue: currentNote
        );

        if (result != null)
        {
            cartItem.Note = result.Trim();

            if (!string.IsNullOrWhiteSpace(cartItem.Note))
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Đã lưu",
                    $"Ghi chú: {cartItem.Note}",
                    "OK"
                );
            }
        }
    }

    // Command để xóa ghi chú
    [RelayCommand]
    private async Task RemoveNote(CartItemModel cartItem)
    {
        if (cartItem == null) return;

        bool confirm = await Application.Current.MainPage.DisplayAlert(
            "Xóa ghi chú",
            "Bạn có chắc muốn xóa ghi chú này?",
            "Xóa",
            "Hủy"
        );

        if (confirm)
        {
            cartItem.Note = string.Empty;
        }
    }

    #endregion

    // Cập nhật tổng tiền
    private void UpdateCartSummary()
    {
        Subtotal = CartItems.Sum(item => item.TotalPrice);
        Total = Subtotal;
    }

    // Command thanh toán
    [RelayCommand]
    private async Task Checkout()
    {
        if (CartItems.Count == 0)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Giỏ hàng trống",
                "Vui lòng thêm món ăn trước khi gửi đơn.",
                "OK"
            );
            return;
        }

        bool confirm = await Application.Current.MainPage.DisplayAlert(
            "Xác nhận gửi đơn",
            $"Xác nhận gửi đơn {Total:N0} VNĐ cho {TenBan}?",
            "Đồng ý",
            "Hủy"
        );

        if (!confirm) return;

        bool success = false;
        try
        {
            var token = await SecureStorage.Default.GetAsync("auth_token");
            if (string.IsNullOrEmpty(token))
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Phiên đăng nhập hết hạn.", "OK");
                return;
            }
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Payload với ghi chú
            var orderPayload = new
            {
                
                Items = CartItems.Select(ci => new
                {
                    FoodId = ci.FoodItem.Id,
                    ci.Quantity,
                    Note = ci.Note
                })
            };

            string jsonPayload = JsonSerializer.Serialize(orderPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ApiConfig.SubmitOrder, content);
            success = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Lỗi Gửi Đơn",
                $"Không thể gửi API: {ex.Message}",
                "OK"
            );
            success = false;
        }

        if (success)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Thành công",
                $"Đã xác nhận đơn hàng cho {TenBan}.\nTổng tiền: {Total:N0} VNĐ",
                "OK"
            );
            CartItems.Clear();
            UpdateCartSummary();
            await Application.Current.MainPage.Navigation.PopAsync();
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert(
                "Lỗi",
                "Không thể gửi đơn hàng. Vui lòng thử lại.",
                "OK"
            );
        }
    }

    // Command hủy đơn
    [RelayCommand]
    private async Task CancelOrder()
    {
        bool confirm = await Application.Current.MainPage.DisplayAlert(
            "Xác nhận hủy đơn",
            "Bạn có chắc chắn muốn hủy tất cả món đã chọn?",
            "Đồng ý",
            "Không"
        );

        if (confirm)
        {
            CartItems.Clear();
            UpdateCartSummary();
        }
    }

    // HttpClient Handler
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