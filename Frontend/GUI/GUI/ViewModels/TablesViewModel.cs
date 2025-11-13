using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Maui.Controls;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class TablesViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        [ObservableProperty]
        private ObservableCollection<Ban> _tables;

        // Thuộc tính này rất quan trọng, giữ nguyên
        public Page CurrentPage { get; set; }

        public TablesViewModel()
        {
            _apiService = new ApiService();
            Tables = new ObservableCollection<Ban>();
        }

        [RelayCommand]
        public async Task LoadTablesAsync()
        {
            // Không cần try-catch ở đây,
            // TablesPage.xaml.cs (OnAppearing) đã xử lý
            var tableList = await _apiService.GetTablesAsync();

            Tables.Clear();
            foreach (var table in tableList)
            {
                Tables.Add(table);
            }
        }

        // --- HÀM SELECTTABLE ĐÃ ĐƯỢC CẬP NHẬT HOÀN TOÀN ---
        [RelayCommand]
        private async Task SelectTable(Ban selectedTable)
        {
            // Thêm kiểm tra an toàn cho Application.Current.MainPage
            if (selectedTable == null || Application.Current.MainPage == null) return;

            try
            {
                // LUỒNG 1: Nếu bàn "Trống" (Theo JSON API của bạn)
                if (selectedTable.TrangThai == "Bàn trống")
                {
                    // SỬA LỖI: Truyền MaBan và dùng Application.Current.MainPage
                    await Application.Current.MainPage.Navigation.PushAsync(new OrdersPage());
                    return;
                }

                // ---
                // LUỒNG 2: Nếu bàn KHÔNG trống
                // ---
                string action = await Application.Current.MainPage.DisplayActionSheet(
                    $"Bàn: {selectedTable.TenBan} (Hiện tại: {selectedTable.TrangThai})",
                    "Hủy",
                    null,
                    // Các lựa chọn mới theo yêu cầu
                    "Thanh toán",
                    "Xem / Thêm món",
                    "Đổi trạng thái bàn"
                );

                switch (action)
                {
                    case "Thanh toán":
                        // Giả định: Điều hướng đến BillGenerationPage
                        await Application.Current.MainPage.Navigation.PushAsync(new BillGenerationPage());
                        break;

                    case "Xem / Thêm món":
                        // Điều hướng đến OrdersPage
                        await Application.Current.MainPage.Navigation.PushAsync(new OrdersPage());
                        break;

                    case "Đổi trạng thái bàn":
                        // Gọi hàm riêng để xử lý việc đổi trạng thái
                        await ShowChangeStatusOptions(selectedTable);
                        break;

                    case "Hủy":
                    default:
                        return; // Không làm gì cả
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi SelectTable: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Đã xảy ra lỗi, vui lòng thử lại.", "OK");
            }
        }

        /// <summary>
        /// Hàm trợ giúp (helper) để hiển thị menu đổi trạng thái
        /// </summary>
        private async Task ShowChangeStatusOptions(Ban selectedTable)
        {
            var possibleStatuses = new[] { "Bàn bận", "Bàn trống", "Bàn đã đặt" };
            var otherStatuses = possibleStatuses
                .Where(s => s != selectedTable.TrangThai)
                .ToArray();

            // SỬA LỖI: Dùng Application.Current.MainPage
            string statusAction = await Application.Current.MainPage.DisplayActionSheet(
                $"Chọn trạng thái mới cho {selectedTable.TenBan}",
                "Hủy",
                null,
                otherStatuses
            );

            if (statusAction == null || statusAction == "Hủy")
                return;

            string newStatus = statusAction;

            try
            {
                bool success = await _apiService.UpdateTableStatusAsync(selectedTable.MaBan, newStatus);

                if (success)
                {
                    await LoadTablesAsync(); // Tải lại để cập nhật màu
                }
                else
                {
                    // SỬA LỖI: Dùng Application.Current.MainPage
                    await Application.Current.MainPage.DisplayAlert("Lỗi", "Không thể cập nhật trạng thái bàn.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi cập nhật trạng thái: {ex.Message}");
                // SỬA LỖI: Dùng Application.Current.MainPage
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Có lỗi xảy ra khi cập nhật.", "OK");
            }
        }
    }
}