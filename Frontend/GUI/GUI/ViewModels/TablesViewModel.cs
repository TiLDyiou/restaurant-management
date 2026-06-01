using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Constants;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class TablesViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private readonly TableService _tableService;
        private readonly JsonSerializerOptions _jsonOptions;
        private List<Ban> _allTables = new();

        private string _currentFilter = "Tất cả";

        [ObservableProperty]
        private ObservableCollection<Ban> filteredTables = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNewNotifications))]
        private int newNotificationCount = 0;
        public bool HasNewNotifications => NewNotificationCount > 0;

        [ObservableProperty]
        private ObservableCollection<string> notificationList = new();

        [ObservableProperty]
        private bool showNotificationPopup;

        public event EventHandler DataUpdated;

        public TablesViewModel(HttpClient httpClient, TableService tableService)
        {
            _httpClient = httpClient;
            _tableService = tableService;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task LoadTablesAsync()
        {
            try
            {
                var tables = await _tableService.GetAllTablesAsync();
                _allTables = tables ?? new List<Ban>();
                FilterTables(_currentFilter);
            }
            catch (Exception ex) { Console.WriteLine($"LoadTables Error: {ex.Message}"); }
        }

        public async Task LoadNotificationsAsync()
        {
            try
            {
                string url = $"{ApiConfig.Notifications}?loai=PHUCVU";

                var response = await _httpClient.GetFromJsonAsync<ApiResponse<PaginatedResult<ThongBaoDto>>>(url, _jsonOptions);

                if (response != null && response.Success && response.Data != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        NotificationList.Clear();
                        if (response.Data.Items != null)
                        {
                            foreach (var item in response.Data.Items)
                            {
                                string time = item.ThoiGian.ToString("HH:mm");
                                NotificationList.Add($"{item.NoiDung} ({time})");
                            }
                        }
                        NewNotificationCount = NotificationList.Count;
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tải thông báo: {ex.Message}");
            }
        }

        public void SubscribeSocket()
        {
            _ = TCPSocketClient.Instance.ConnectAsync();
            TCPSocketClient.Instance.OnTableStatusChanged -= HandleTableUpdate;
            TCPSocketClient.Instance.OnTableStatusChanged += HandleTableUpdate;

            TCPSocketClient.Instance.OnDishDone -= HandleDishDone;
            TCPSocketClient.Instance.OnDishDone += HandleDishDone;
        }

        public void UnsubscribeSocket()
        {
            TCPSocketClient.Instance.OnTableStatusChanged -= HandleTableUpdate;
            TCPSocketClient.Instance.OnDishDone -= HandleDishDone;
        }

        private void HandleTableUpdate(string json)
        {
            try
            {
                var updatePayload = JsonSerializer.Deserialize<TableUpdatePayload>(json, _jsonOptions);
                if (updatePayload != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var table = _allTables.FirstOrDefault(t => t.MaBan == updatePayload.MaBan);
                        if (table != null)
                        {
                            table.TrangThai = updatePayload.TrangThai;
                            FilterTables(_currentFilter);
                            DataUpdated?.Invoke(this, EventArgs.Empty);
                        }
                    });
                }
            }
            catch { }
        }

        private void HandleDishDone(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NewNotificationCount++;
                string time = DateTime.Now.ToString("HH:mm");
                NotificationList.Insert(0, $"{message} ({time})");
            });
        }

        [RelayCommand]
        public void FilterTables(string filterType)
        {
            _currentFilter = filterType;
            if (_allTables == null) return;

            IEnumerable<Ban> result = _allTables;

            if (filterType == "Bàn trống")
                result = _allTables.Where(t => t.TrangThai == SystemConstants.TableEmpty);
            else if (filterType == "Bàn bận")
                result = _allTables.Where(t => t.TrangThai == SystemConstants.TableOccupied);
            else if (filterType == "Bàn đã đặt")
                result = _allTables.Where(t => t.TrangThai == SystemConstants.TableReserved);

            var sortedResult = result.OrderBy(t => {
                var match = System.Text.RegularExpressions.Regex.Match(t.TenBan ?? "", @"\d+");
                return match.Success ? int.Parse(match.Value) : int.MaxValue;
            }).ThenBy(t => t.TenBan);

            FilteredTables = new ObservableCollection<Ban>(sortedResult);
            DataUpdated?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        async Task ClearAllNotifications()
        {
            NotificationList.Clear();
            ShowNotificationPopup = false;
            NewNotificationCount = 0;

            try
            {
                string url = $"{ApiConfig.Notifications}?loai=PHUCVU";
                await _httpClient.DeleteAsync(url);
            }
            catch (Exception ex) { Console.WriteLine($"Clear Error: {ex.Message}"); }
        }

        [RelayCommand]
        void ToggleNotifications()
        {
            ShowNotificationPopup = !ShowNotificationPopup;
            if (ShowNotificationPopup)
                NewNotificationCount = 0;
        }

        // ==============================================================================
        // MVVM COMMANDS FOR ADVANCED TABLE OPERATIONS
        // ==============================================================================

        [RelayCommand]
        public async Task UpdateTableStatusAsync(Ban table)
        {
            if (table == null) return;
            string status = await Application.Current.MainPage.DisplayActionSheet(
                $"Trạng thái {table.TenBan}", 
                "Hủy", 
                null, 
                "Trống", "Có khách", "Bàn đã đặt");
                
            if (string.IsNullOrEmpty(status) || status == "Hủy") return;

            try
            {
                bool success = await _tableService.UpdateStatusAsync(table.MaBan, status);
                if (success)
                {
                    table.TrangThai = status;
                    FilterTables(_currentFilter);
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Lỗi", "Cập nhật thất bại", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", ex.Message, "OK");
            }
        }

        [RelayCommand]
        public async Task MergeTablesAsync(Ban table)
        {
            if (table == null) return;
            try
            {
                var vacantTables = _allTables
                    .Where(t => t.TrangThai == SystemConstants.TableEmpty && t.MaBan != table.MaBan && !t.IsMerged)
                    .ToList();

                if (vacantTables.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Thông báo", "Không có bàn trống nào khả dụng để gộp.", "OK");
                    return;
                }

                var options = vacantTables.Select(t => t.TenBan).ToArray();
                var choice = await Application.Current.MainPage.DisplayActionSheet($"Gộp {table.TenBan} với bàn:", "Hủy", null, options);

                if (string.IsNullOrEmpty(choice) || choice == "Hủy") return;

                var targetTable = vacantTables.FirstOrDefault(t => t.TenBan == choice);
                if (targetTable != null)
                {
                    bool confirm = await Application.Current.MainPage.DisplayAlert("Xác nhận gộp bàn", $"Bạn có chắc chắn muốn gộp {table.TenBan} và {targetTable.TenBan} làm một không?", "Gộp ngay", "Hủy");
                    if (!confirm) return;

                    bool success = await _tableService.MergeTablesAsync(table.MaBan, targetTable.MaBan);
                    if (success)
                    {
                        await Application.Current.MainPage.DisplayAlert("Thành công", $"Đã gộp thành công {table.TenBan} và {targetTable.TenBan}.", "OK");
                        await LoadTablesAsync();
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Lỗi", "Gộp bàn thất bại. Vui lòng thử lại sau.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi hệ thống", ex.Message, "OK");
            }
        }

        [RelayCommand]
        public async Task SplitTablesAsync(Ban table)
        {
            if (table == null) return;
            try
            {
                bool confirm = await Application.Current.MainPage.DisplayAlert("Xác nhận tách bàn", $"Bạn có chắc chắn muốn giải phóng liên kết gộp của {table.TenBan} không?", "Tách ngay", "Hủy");
                if (!confirm) return;

                bool success = await _tableService.SplitTablesAsync(table.MaBan);
                if (success)
                {
                    await Application.Current.MainPage.DisplayAlert("Thành công", $"Đã tách liên kết gộp của {table.TenBan} thành công.", "OK");
                    await LoadTablesAsync();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Lỗi", "Tách bàn gộp thất bại.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi hệ thống", ex.Message, "OK");
            }
        }

        [RelayCommand]
        public async Task TransferOrderAsync(Ban table)
        {
            if (table == null) return;
            try
            {
                var vacantTables = _allTables
                    .Where(t => t.TrangThai == SystemConstants.TableEmpty && !t.IsMerged)
                    .ToList();

                if (vacantTables.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Thông báo", "Không có bàn trống nào khả dụng để chuyển đến.", "OK");
                    return;
                }

                var options = vacantTables.Select(t => t.TenBan).ToArray();
                var choice = await Application.Current.MainPage.DisplayActionSheet($"Chuyển đơn từ {table.TenBan} đến bàn:", "Hủy", null, options);

                if (string.IsNullOrEmpty(choice) || choice == "Hủy") return;

                var targetTable = vacantTables.FirstOrDefault(t => t.TenBan == choice);
                if (targetTable != null)
                {
                    bool confirm = await Application.Current.MainPage.DisplayAlert("Xác nhận chuyển bàn", $"Bạn có muốn chuyển toàn bộ đơn hàng từ {table.TenBan} sang {targetTable.TenBan} không?", "Chuyển ngay", "Hủy");
                    if (!confirm) return;

                    bool success = await _tableService.TransferOrderAsync(table.MaBan, targetTable.MaBan);
                    if (success)
                    {
                        await Application.Current.MainPage.DisplayAlert("Thành công", $"Đã chuyển đơn hàng sang {targetTable.TenBan} thành công.", "OK");
                        await LoadTablesAsync();
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Lỗi", "Chuyển bàn thất bại.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi hệ thống", ex.Message, "OK");
            }
        }

        [RelayCommand]
        public async Task ShowTableHistoryAsync(Ban table)
        {
            if (table == null) return;
            try
            {
                var history = await _tableService.GetTableHistoryAsync(table.MaBan);
                if (history == null || history.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Nhật ký bàn ăn", $"Chưa có nhật ký hoạt động nào cho {table.TenBan}.", "OK");
                    return;
                }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"--- NHẬT KÝ HOẠT ĐỘNG: {table.TenBan} ---");
                sb.AppendLine();
                foreach (var log in history.OrderByDescending(h => h.ThoiGian).Take(10))
                {
                    string timeStr = log.ThoiGian.ToString("dd/MM/yyyy HH:mm");
                    string nhanVien = string.IsNullOrEmpty(log.TenNV) ? "Hệ thống" : log.TenNV;
                    string oldStatus = string.IsNullOrEmpty(log.TrangThaiCu) ? "Trống" : log.TrangThaiCu;
                    sb.AppendLine($"🕒 {timeStr}");
                    sb.AppendLine($"   → {oldStatus} ➔ {log.TrangThaiMoi}");
                    sb.AppendLine($"   👤 Thực hiện: {nhanVien}");
                    sb.AppendLine();
                }

                await Application.Current.MainPage.DisplayAlert("Lịch sử bàn ăn", sb.ToString(), "Đóng");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", $"Không thể tải lịch sử bàn: {ex.Message}", "OK");
            }
        }

        public bool IsAdmin => UserState.IsAdmin;

        [RelayCommand]
        public async Task AddTableAsync()
        {
            try
            {
                string maBan = await Application.Current.MainPage.DisplayPromptAsync("Thêm bàn mới", "Nhập mã bàn (tối đa 5 ký tự):", "Lưu", "Hủy", "B000", 5);
                if (string.IsNullOrWhiteSpace(maBan)) return;
                maBan = maBan.Trim();

                string tenBan = await Application.Current.MainPage.DisplayPromptAsync("Thêm bàn mới", "Nhập tên bàn (Ví dụ: Bàn 20):", "Lưu", "Hủy", $"Bàn {maBan.Replace("B", "")}");
                if (string.IsNullOrWhiteSpace(tenBan)) return;
                tenBan = tenBan.Trim();

                string sucChuaStr = await Application.Current.MainPage.DisplayPromptAsync("Thêm bàn mới", "Nhập sức chứa (1 - 100):", "Lưu", "Hủy", "4", 3, Keyboard.Numeric);
                if (string.IsNullOrWhiteSpace(sucChuaStr)) return;
                if (!int.TryParse(sucChuaStr, out int sucChua) || sucChua < 1 || sucChua > 100)
                {
                    await Application.Current.MainPage.DisplayAlert("Lỗi", "Sức chứa không hợp lệ (phải từ 1 đến 100).", "OK");
                    return;
                }

                string khuVuc = await Application.Current.MainPage.DisplayPromptAsync("Thêm bàn mới", "Nhập khu vực (Ví dụ: Tầng 1):", "Lưu", "Hủy", "Tầng 1");
                if (string.IsNullOrWhiteSpace(khuVuc)) return;
                khuVuc = khuVuc.Trim();

                var newTableDto = new CreateBanDto
                {
                    MaBan = maBan,
                    TenBan = tenBan,
                    SucChua = sucChua,
                    KhuVuc = khuVuc
                };

                bool success = await _tableService.CreateTableAsync(newTableDto);
                if (success)
                {
                    await Application.Current.MainPage.DisplayAlert("Thành công", $"Đã thêm {tenBan} thành công.", "OK");
                    await LoadTablesAsync();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Lỗi", "Không thể thêm bàn. Mã bàn có thể đã tồn tại.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi hệ thống", ex.Message, "OK");
            }
        }

        [RelayCommand]
        public async Task DeleteTableAsync(Ban table)
        {
            if (table == null) return;
            try
            {
                bool confirm = await Application.Current.MainPage.DisplayAlert("Xác nhận xóa bàn", $"Bạn có chắc chắn muốn xóa vĩnh viễn {table.TenBan} khỏi hệ thống không?", "Xóa ngay", "Hủy");
                if (!confirm) return;

                bool success = await _tableService.DeleteTableAsync(table.MaBan);
                if (success)
                {
                    await Application.Current.MainPage.DisplayAlert("Thành công", $"Đã xóa {table.TenBan} thành công.", "OK");
                    await LoadTablesAsync();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Lỗi", "Không thể xóa bàn này.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi hệ thống", ex.Message, "OK");
            }
        }
    }
}