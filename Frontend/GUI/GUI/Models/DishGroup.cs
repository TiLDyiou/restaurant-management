using System.Collections.Generic;

namespace RestaurantManagementGUI.Models
{
    // Kế thừa List<FoodModel> để binding trực tiếp vào list con
    public class DishGroup : List<FoodModel>
    {
        public string Category { get; private set; }

        public DishGroup(string category, List<FoodModel> dishes) : base(dishes)
        {
            Category = category;
        }
    }
}