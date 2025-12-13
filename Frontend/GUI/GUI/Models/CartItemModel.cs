using CommunityToolkit.Mvvm.ComponentModel;

namespace RestaurantManagementGUI.Models
{
    public partial class CartItemModel : ObservableObject
    {
        public FoodModel FoodItem { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPrice))] // Tự động cập nhật TotalPrice khi Quantity đổi
        private int _quantity;

        public decimal TotalPrice => FoodItem != null ? FoodItem.Price * Quantity : 0;

        [ObservableProperty]
        private string _note;
    }
}