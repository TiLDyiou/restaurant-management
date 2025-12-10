namespace RestaurantManagementGUI
{
    public partial class BillGenerationPage : ContentPage
    {
        public BillGenerationPage()
        {
            InitializeComponent();
            BindingContext = new ViewModels.BillGenerationViewModel();
        }
    }
}