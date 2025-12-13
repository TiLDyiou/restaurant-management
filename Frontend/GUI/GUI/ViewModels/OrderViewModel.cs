using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using System.Collections.ObjectModel;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class OrderViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly IUserSession _userSession;

        // Dữ liệu gốc
        private List<FoodModel> _allFoods = new();

        // Collections hiển thị
        public ObservableCollection<FoodModel> DisplayedFoods { get; } = new();
        public ObservableCollection<string> Categories { get; } = new();
        public ObservableCollection<CartItemModel> CartItems { get; } = new();

        // Properties
        [ObservableProperty]
        private string _searchText;

        [ObservableProperty]
        private string _selectedCategory = "Tất cả";

        [ObservableProperty]
        private string _tableName = "Đơn tự do";

        [ObservableProperty]
        private string _tableId = "B01"; // Mặc định nếu không chọn bàn

        [ObservableProperty]
        private decimal _totalAmount;

        public OrderViewModel(ApiService apiService, IUserSession userSession)
        {
            _apiService = apiService;
            _userSession = userSession;
        }

        // --- COMMANDS ---

        [RelayCommand]
        public async Task LoadMenuAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            var response = await _apiService.GetAsync<List<FoodModel>>(ApiConfig.Dishes);
            if (response.Success && response.Data != null)
            {
                _allFoods = response.Data;

                // Tạo danh sách Category
                Categories.Clear();
                Categories.Add("Tất cả");
                var cats = _allFoods.Select(f => f.Category).Distinct().OrderBy(c => c);
                foreach (var c in cats) Categories.Add(c);

                FilterFoods();
            }
            IsBusy = false;
        }

        [RelayCommand]
        public void FilterCategory(string category)
        {
            SelectedCategory = category;
            FilterFoods();
        }

        partial void OnSearchTextChanged(string value) => FilterFoods(); // Auto filter khi gõ chữ

        private void FilterFoods()
        {
            var query = _allFoods.AsEnumerable();

            if (SelectedCategory != "Tất cả")
            {
                query = query.Where(f => f.Category == SelectedCategory);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(f => f.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            DisplayedFoods.Clear();
            foreach (var item in query) DisplayedFoods.Add(item);
        }

        // --- CART LOGIC ---

        [RelayCommand]
        public void AddToCart(FoodModel food)
        {
            if (food == null) return;

            var existingItem = CartItems.FirstOrDefault(c => c.FoodItem.Id == food.Id);
            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                CartItems.Add(new CartItemModel { FoodItem = food, Quantity = 1 });
            }
            RecalculateTotal();
        }

        [RelayCommand]
        public void IncreaseQty(CartItemModel item)
        {
            if (item == null) return;
            item.Quantity++;
            RecalculateTotal();
        }

        [RelayCommand]
        public void DecreaseQty(CartItemModel item)
        {
            if (item == null) return;
            if (item.Quantity > 1)
            {
                item.Quantity--;
            }
            else
            {
                CartItems.Remove(item);
            }
            RecalculateTotal();
        }

        [RelayCommand]
        public void ClearCart()
        {
            CartItems.Clear();
            RecalculateTotal();
        }

        private void RecalculateTotal()
        {
            TotalAmount = CartItems.Sum(c => c.TotalPrice);
        }

        // --- SUBMIT ORDER ---

        [RelayCommand]
        public async Task SubmitOrderAsync()
        {
            if (CartItems.Count == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Thông báo", "Giỏ hàng trống!", "OK");
                return;
            }

            bool confirm = await Application.Current.MainPage.DisplayAlert("Xác nhận",
                $"Gửi đơn cho {TableName} gồm {CartItems.Count} món?", "Gửi ngay", "Hủy");

            if (!confirm) return;

            IsBusy = true;

            var orderDto = new CreateHoaDonDto
            {
                MaBan = TableId,
                MaNV = _userSession.MaNV,
                ChiTietHoaDons = CartItems.Select(c => new ChiTietHoaDonDto
                {
                    MaMA = c.FoodItem.Id,
                    SoLuong = c.Quantity
                }).ToList()
            };

            var response = await _apiService.PostAsync<object>(ApiConfig.Orders, orderDto);

            if (response.Success)
            {
                await Application.Current.MainPage.DisplayAlert("Thành công", "Đơn hàng đã gửi xuống bếp! 👨‍🍳", "OK");
                ClearCart();
                await Application.Current.MainPage.Navigation.PopAsync(); // Quay lại trang trước
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", response.Message, "OK");
            }

            IsBusy = false;
        }
    }
}