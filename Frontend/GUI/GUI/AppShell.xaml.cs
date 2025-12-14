
using RestaurantManagementGUI.Views;

namespace RestaurantManagementGUI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(FoodMenuPage), typeof(FoodMenuPage));
            Routing.RegisterRoute(nameof(TablesPage), typeof(TablesPage));
            Routing.RegisterRoute("ChefOrdersPage", typeof(ChefOrdersPage));
        }

    }
}
