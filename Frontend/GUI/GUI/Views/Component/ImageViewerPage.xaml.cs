namespace RestaurantManagementGUI
{
    public partial class ImageViewerPage : ContentPage
    {
        private double currentScale = 1;
        private double startScale = 1;

        public ImageViewerPage(string imageUrl)
        {
            InitializeComponent();

            // Gán nguồn ảnh
            DetailImage.Source = imageUrl;
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            // Đóng trang Modal
            await Navigation.PopModalAsync();
        }

        private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            if (e.Status == GestureStatus.Started)
            {
                startScale = DetailImage.Scale;
            }
            else if (e.Status == GestureStatus.Running)
            {
                // Tính toán tỷ lệ zoom
                currentScale += (e.Scale - 1) * startScale;
                currentScale = Math.Max(1, currentScale); // Không cho zoom nhỏ hơn kích thước gốc

                // Apply zoom
                DetailImage.Scale = currentScale;
            }
        }
    }
}