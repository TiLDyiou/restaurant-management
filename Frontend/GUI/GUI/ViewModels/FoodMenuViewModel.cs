// File: ViewModels/FoodMenuViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;

namespace RestaurantManagementGUI
{
    public class FoodMenuViewModel : BindableObject
    {
        private readonly HttpClient _httpClient;

        public FoodMenuViewModel()
        {
            _httpClient = new HttpClient();

            Categories = new ObservableCollection<string>();
            DisplayedFoodItems = new ObservableCollection<FoodModel>();
            AllFoodItems = new ObservableCollection<FoodModel>();
            CartItems = new ObservableCollection<CartItemModel>();

            // Commands
            AddToCartCommand = new Command<FoodModel>(AddToCart);
            IncreaseQuantityCommand = new Command<CartItemModel>(IncreaseQuantity);
            DecreaseQuantityCommand = new Command<CartItemModel>(DecreaseQuantity);
            AddNoteCommand = new Command<CartItemModel>(AddNote);
            RemoveNoteCommand = new Command<CartItemModel>(RemoveNote);
            FilterCommand = new Command<string>(FilterByCategory);
            CheckoutCommand = new Command(Checkout);
            CancelOrderCommand = new Command(CancelOrder);
        }

        #region Properties

        private string _tenBan = "Đơn tự do";
        public string TenBan
        {
            get => _tenBan;
            set { _tenBan = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Categories { get; set; }
        public ObservableCollection<FoodModel> AllFoodItems { get; set; }
        public ObservableCollection<FoodModel> DisplayedFoodItems { get; set; }
        public ObservableCollection<CartItemModel> CartItems { get; set; }

        private string _selectedCategory = "Tất cả";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); FilterFoodItems(); }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); FilterFoodItems(); }
        }

        public decimal Subtotal => CartItems.Sum(c => c.TotalPrice);
        public decimal Total => Subtotal; // Có thể thêm thuế/phí sau này

        #endregion

        #region Commands

        public ICommand AddToCartCommand { get; }
        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand AddNoteCommand { get; }
        public ICommand RemoveNoteCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand CheckoutCommand { get; }
        public ICommand CancelOrderCommand { get; }

        #endregion

        #region Methods

        // Load thực đơn từ API
        public async Task InitializeAsync()
        {
            try
            {
                var items = await _httpClient.GetFromJsonAsync<FoodModel[]>(ApiConfig.GetFoodMenu);
                if (items != null)
                {
                    AllFoodItems.Clear();
                    foreach (var food in items)
                        AllFoodItems.Add(food);

                    // Lấy danh sách category
                    var cats = items.Select(f => f.Category).Distinct().OrderBy(c => c).ToList();
                    Categories.Clear();
                    Categories.Add("Tất cả");
                    foreach (var c in cats) Categories.Add(c);

                    FilterFoodItems();
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", $"Không thể tải thực đơn: {ex.Message}", "OK");
            }
        }

        private void FilterFoodItems()
        {
            var filtered = AllFoodItems.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "Tất cả")
                filtered = filtered.Where(f => f.Category == SelectedCategory);

            if (!string.IsNullOrWhiteSpace(SearchText))
                filtered = filtered.Where(f => f.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            DisplayedFoodItems.Clear();
            foreach (var f in filtered)
                DisplayedFoodItems.Add(f);

            OnPropertyChanged(nameof(DisplayedFoodItems));
        }

        private void FilterByCategory(string category)
        {
            SelectedCategory = category;
        }

        private void AddToCart(FoodModel food)
        {
            var item = CartItems.FirstOrDefault(c => c.FoodItem.Id == food.Id);
            if (item == null)
            {
                CartItems.Add(new CartItemModel
                {
                    FoodItem = food,
                    Quantity = 1
                });
            }
            else
            {
                item.Quantity++;
            }
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Total));
        }

        private void IncreaseQuantity(CartItemModel item)
        {
            item.Quantity++;
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Total));
        }

        private void DecreaseQuantity(CartItemModel item)
        {
            if (item.Quantity > 1)
                item.Quantity--;
            else
                CartItems.Remove(item);

            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Total));
        }

        private async void AddNote(CartItemModel item)
        {
            string result = await Application.Current.MainPage.DisplayPromptAsync("Ghi chú", $"Nhập ghi chú cho {item.FoodItem.Name}:", "OK", "Hủy", placeholder: item.Note);
            if (!string.IsNullOrWhiteSpace(result))
            {
                item.Note = result;
                OnPropertyChanged(nameof(CartItems));
            }
        }

        private void RemoveNote(CartItemModel item)
        {
            item.Note = null;
            OnPropertyChanged(nameof(CartItems));
        }

        private async void Checkout()
        {
            if (CartItems.Count == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Thông báo", "Giỏ hàng trống", "OK");
                return;
            }

            // TODO: Gọi API SubmitOrder
            bool confirmed = await Application.Current.MainPage.DisplayAlert("Xác nhận", $"Bạn có muốn gửi {CartItems.Count} món không?", "OK", "Hủy");
            if (confirmed)
            {
                // Gửi đơn lên API (tạm thời log ra console)
                Console.WriteLine("Đơn hàng đã gửi:");
                foreach (var c in CartItems)
                    Console.WriteLine($"{c.FoodItem.Name} x{c.Quantity} ({c.Note})");

                await Application.Current.MainPage.DisplayAlert("Thành công", "Đơn hàng đã gửi", "OK");
                CancelOrder(); // Xóa giỏ sau khi gửi
            }
        }

        private void CancelOrder()
        {
            CartItems.Clear();
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Total));
        }

        #endregion
    }
}
