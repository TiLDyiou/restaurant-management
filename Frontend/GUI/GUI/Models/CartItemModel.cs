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

        // THÊM MỚI: Property cho ghi chú món ăn
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNote))] // ← QUAN TRỌNG: Notify khi Note thay đổi
        string _note = string.Empty;

        // Property hiển thị có ghi chú hay không
        public bool HasNote => !string.IsNullOrWhiteSpace(Note);

        public decimal TotalPrice => FoodItem.Price * Quantity;

        public string DisplayName => $"{Quantity} x {FoodItem.Name}";
    }
}