namespace RestaurantManagementGUI
{
    public partial class BillGenerationPage : ContentPage
    {
        public BillGenerationPage()
        {
            InitializeComponent();
            BindingContext = new ViewModels.BillGenerationViewModel();
        }

        private void OnPaymentMethodChanged(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value)
            {
                var radio = sender as RadioButton;
                if (radio.Value.ToString() == "Cash")
                {
                    CashSection.IsVisible = true;
                    TransferSection.IsVisible = false;
                }
                else
                {
                    CashSection.IsVisible = false;
                    TransferSection.IsVisible = true;
                }
            }
        }
    }
}