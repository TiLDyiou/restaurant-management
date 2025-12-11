using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Helpers; // Cần thiết để dùng UserState
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.ViewModels;
using RestaurantManagementGUI.Views;
using System.Diagnostics;

namespace RestaurantManagementGUI
{
    public partial class DashboardPage : ContentPage
    {
        public DashboardPage()
        {
            InitializeComponent();
            LoadUserInfo();
        }

        private async void LoadUserInfo()
        {
            try
            {
                // 1. Lấy dữ liệu từ bộ nhớ máy (SecureStorage)
                string username = await SecureStorage.Default.GetAsync("user_username") ?? "User";
                string role = await SecureStorage.Default.GetAsync("user_role") ?? "Staff";
                string maNV = await SecureStorage.Default.GetAsync("user_manv") ?? "";
                // Nếu không có tên hiển thị thì lấy username
                string tenNV = await SecureStorage.Default.GetAsync("user_hoten") ?? username;

                // 2. Hiển thị lời chào trên giao diện
                WelcomeLabel.Text = $"Xin chào, {tenNV}";

                // ==============================================================
                // 👇👇👇 QUAN TRỌNG: LƯU VÀO USERSTATE ĐỂ DÙNG TOÀN CỤC 👇👇👇
                // ==============================================================

                UserState.CurrentMaNV = maNV;
                UserState.CurrentTenNV = tenNV;

                // Chuẩn hóa quyền (Admin hoặc Staff)
                // Kiểm tra xem trong chuỗi role có chứa chữ "admin" hoặc "quanly" không (không phân biệt hoa thường)
                string roleLower = role.ToLower();
                if (roleLower.Contains("admin") || roleLower.Contains("quanly"))
                {
                    UserState.CurrentRole = "Admin";
                }
                else
                {
                    UserState.CurrentRole = "Staff";
                }

                // Debug để kiểm tra xem đã nhận đúng chưa
                Debug.WriteLine($"[DASHBOARD] User: {tenNV} | Role Gốc: {role} -> Role Hệ thống: {UserState.CurrentRole} | MaNV: {UserState.CurrentMaNV}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DASHBOARD ERROR] {ex.Message}");
                WelcomeLabel.Text = "Welcome!";
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                bool confirm = await DisplayAlert("Đăng xuất", "Bạn có chắc chắn muốn đăng xuất?", "Có", "Không");
                if (!confirm) return;

                // Xóa dữ liệu lưu trữ
                SecureStorage.RemoveAll();

                // Xóa dữ liệu phiên làm việc hiện tại
                UserState.Clear();

                // Ngắt kết nối Socket (Nếu có)
                if (SocketListener.Instance != null)
                {
                    SocketListener.Instance.Disconnect();
                }

                // Quay về trang đăng nhập
                Application.Current.MainPage = new NavigationPage(new LoginPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Đăng xuất thất bại: {ex.Message}", "OK");
            }
        }

        // Users: admin mở UsersPage, còn các chức vụ khác mở profile
        private async void OnUsersClicked(object sender, EventArgs e)
        {
            try
            {
                // Kiểm tra quyền từ UserState luôn cho chuẩn
                if (UserState.CurrentRole == "Admin")
                {
                    await Navigation.PushAsync(new UsersPage());
                }
                else
                {
                    await Navigation.PushAsync(new ChefAndUserProfilePage());
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Không thể mở trang: {ex.Message}", "OK");
            }
        }

        private async void OnFoodCategoriesClicked(object sender, EventArgs e) => await Navigation.PushAsync(new QuanLyMonAnPage());

        private async void OnFoodMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new FoodMenuPage());

        private async void OnOrdersClicked(object sender, EventArgs e) => await Navigation.PushAsync(new OrdersPage());

        private async void OnTablesClicked(object sender, EventArgs e)
        {
            // Lấy instance từ DI container nếu có, hoặc tạo mới
            var page = Handler?.MauiContext?.Services.GetService<TablesPage>() ?? new TablesPage();
            await Navigation.PushAsync(page);
        }

        private async void OnBillGenerationClicked(object sender, EventArgs e) => await Navigation.PushAsync(new BillGenerationPage());

        private async void OnRevenueReportClicked(object sender, EventArgs e) => await Navigation.PushAsync(new RevenueReportPage());

        private async void OnChatClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ChatPage());

        protected override void OnAppearing()
        {
            base.OnAppearing();
            AdjustButtonSizes();
        }

        // Hàm chỉnh kích thước nút bấm cho đẹp trên các màn hình khác nhau
        private void AdjustButtonSizes()
        {
            try
            {
                var screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
                double availableWidth = screenWidth - 160 - 60; // Trừ đi lề và khoảng cách
                double buttonWidth = screenWidth >= 1000 ? availableWidth / 4 : availableWidth / 2;

                if (ButtonsLayout != null)
                {
                    foreach (var child in ButtonsLayout.Children)
                    {
                        if (child is Border border)
                        {
                            border.WidthRequest = buttonWidth;

                            if (border.Content is VerticalStackLayout stack && stack.Children.Count > 1 && stack.Children[1] is Button btn)
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