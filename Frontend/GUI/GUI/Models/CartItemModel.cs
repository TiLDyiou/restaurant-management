using System;
using Microsoft.Maui.Graphics;

namespace RestaurantManagementGUI.Models
{
    public class CartItemModel : BindableObject
    {
        private int _quantity = 1;
        private string? _note;

        public FoodModel FoodItem { get; set; } = new();

        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPrice)); }
        }

        public string? Note
        {
            get => _note;
            set { _note = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNote)); }
        }

        public bool HasNote => !string.IsNullOrWhiteSpace(Note);

        public decimal TotalPrice => FoodItem.Price * Quantity;
    }
}
