namespace RestaurantManagementGUI
{
    public partial class BillGenerationPage : ContentPage
    {
        public BillGenerationPage()
        {
            InitializeComponent();
            BindingContext = new ViewModels.BillGenerationViewModel();
        }

        // Xử lý sự kiện khi chọn RadioButton để ẩn hiện khung Tiền mặt/QR
        private void OnPaymentMethodChanged(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value) // Nếu được chọn
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