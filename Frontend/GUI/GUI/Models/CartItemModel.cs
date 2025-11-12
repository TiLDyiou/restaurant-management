// File: Models/CartItemModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace RestaurantManagementGUI.Models
{
    public partial class CartItemModel : ObservableObject
    {
        public FoodModel FoodItem { get; set; } = new FoodModel();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPrice))]
        int _quantity;

        // SỬA LỖI MODEL: Dùng 'decimal'
        public decimal TotalPrice => FoodItem.Price * Quantity;
        public string DisplayName => $"{Quantity} x {FoodItem.Name}";
    }
}