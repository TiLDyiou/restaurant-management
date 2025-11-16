using System.Collections.ObjectModel;
namespace RestaurantManagementGUI.Models
{
    public class DishGroup : ObservableCollection<Dish>
    {
        public string Category { get; private set; }
        public DishGroup(string category, List<Dish> dishes) : base(dishes)
        {
            Category = category;
        }
    }
}