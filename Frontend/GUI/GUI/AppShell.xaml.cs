namespace RestaurantManagementGUI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(TablesPage), typeof(TablesPage));
        }
    }
}
