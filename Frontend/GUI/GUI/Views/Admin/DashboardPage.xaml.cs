using RestaurantManagementGUI.ViewModels;

namespace RestaurantManagementGUI.Views.Admin
{
    public partial class DashboardPage : ContentPage
    {
        public DashboardPage(DashboardViewModel viewModel)
        {
            InitializeComponent();
            this.BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            AdjustButtonSizes();

            // Cập nhật lại thông tin User mỗi khi trang hiện lên (đề phòng đổi tên/quyền)
            if (BindingContext is DashboardViewModel vm)
            {
                vm.UpdateUserInfo();
            }
        }

        // Logic UI: Giữ nguyên để layout đẹp
        private void AdjustButtonSizes()
        {
            try
            {
                var screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
                double availableWidth = screenWidth - 160 - 60; // Trừ cột trái và padding

                // Màn hình to thì 4 cột, nhỏ thì 2 cột
                double buttonWidth = screenWidth >= 1000 ? availableWidth / 4 : availableWidth / 2;

                if (ButtonsLayout != null)
                {
                    foreach (var child in ButtonsLayout.Children)
                    {
                        if (child is Border border)
                        {
                            border.WidthRequest = buttonWidth;

                            // Chỉnh size nút bên trong border
                            if (border.Content is VerticalStackLayout stack
                                && stack.Children.Count > 1
                                && stack.Children[1] is Button btn)
                            {
                                btn.WidthRequest = buttonWidth - 20;
                                btn.HeightRequest = 80;
                                btn.FontSize = 14;
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}