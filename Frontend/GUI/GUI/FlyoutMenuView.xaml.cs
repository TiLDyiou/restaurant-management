using RestaurantManagementGUI.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RestaurantManagementGUI
{
    public partial class FlyoutMenuView : ContentView
    {
        // Properties để binding từ bên ngoài
        public static readonly BindableProperty SelectedTableProperty =
            BindableProperty.Create(nameof(SelectedTable), typeof(Ban), typeof(FlyoutMenuView), null, propertyChanged: OnSelectedTableChanged);

        public Ban SelectedTable
        {
            get => (Ban)GetValue(SelectedTableProperty);
            set => SetValue(SelectedTableProperty, value);
        }

        // Events để giao tiếp với TablesPage
        public event EventHandler<Ban> ChangeStatusRequested;
        public event EventHandler<Ban> ViewAddOrderRequested;
        public event EventHandler<Ban> PaymentRequested;
        public event EventHandler RefreshRequested;
        public event EventHandler<string> FilterChanged;

        public FlyoutMenuView()
        {
            InitializeComponent();
            StatusFilterPicker.SelectedIndex = 0; // Mặc định: Tất cả bàn
        }

        // Phương thức để mở Flyout
        public async Task OpenAsync()
        {
            FlyoutContainer.IsVisible = true;
            FlyoutContainer.InputTransparent = false;

            // Animation: Fade in overlay + Slide in panel
            await Task.WhenAll(
                Overlay.FadeTo(1, 250, Easing.CubicOut),
                FlyoutPanel.TranslateTo(0, 0, 300, Easing.CubicOut)
            );
        }

        // Phương thức để đóng Flyout
        public async Task CloseAsync()
        {
            // Animation: Slide out panel + Fade out overlay
            await Task.WhenAll(
                FlyoutPanel.TranslateTo(-320, 0, 250, Easing.CubicIn),
                Overlay.FadeTo(0, 250, Easing.CubicIn)
            );

            FlyoutContainer.IsVisible = false;
            FlyoutContainer.InputTransparent = true;
        }

        // Cập nhật UI khi có bàn được chọn
        private static void OnSelectedTableChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (FlyoutMenuView)bindable;
            var oldTable = oldValue as Ban;
            var newTable = newValue as Ban;

            // 1. Hủy đăng ký sự kiện ở bàn cũ (tránh lỗi bộ nhớ)
            if (oldTable != null)
            {
                oldTable.PropertyChanged -= control.OnTablePropertyChanged;
            }

            // 2. Đăng ký lắng nghe sự kiện ở bàn mới
            if (newTable != null)
            {
                newTable.PropertyChanged += control.OnTablePropertyChanged;

                // Cập nhật giao diện ngay lập tức
                control.UpdateFlyoutUI(newTable);

                control.SelectedTableInfo.IsVisible = true;
                control.QuickActionsSection.IsVisible = true;
            }
            else
            {
                control.SelectedTableInfo.IsVisible = false;
                control.QuickActionsSection.IsVisible = false;
            }
        }

        // 3. HÀM MỚI: Xử lý khi thuộc tính của Bàn thay đổi (do SignalR cập nhật)
        private void OnTablePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Chỉ cập nhật nếu cái thay đổi là Trạng Thái hoặc Tên Bàn
            if (e.PropertyName == nameof(Ban.TrangThai) || e.PropertyName == nameof(Ban.TenBan))
            {
                // Bắt buộc chạy trên UI Thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (SelectedTable != null)
                    {
                        UpdateFlyoutUI(SelectedTable);
                    }
                });
            }
        }

        // 4. HÀM MỚI: Gom logic vẽ UI vào một chỗ
        private void UpdateFlyoutUI(Ban table)
        {
            SelectedTableName.Text = table.TenBan;
            SelectedTableStatus.Text = GetStatusEmoji(table.TrangThai) + " " + table.TrangThai;

            // (Tùy chọn) Đổi màu chữ dựa theo trạng thái cho đẹp
            if (table.TrangThai == "Bàn trống")
                SelectedTableStatus.TextColor = Colors.Green;
            else if (table.TrangThai == "Bàn bận")
                SelectedTableStatus.TextColor = Colors.Red;
            else
                SelectedTableStatus.TextColor = Color.FromArgb("#ffbd59"); // Màu vàng cũ
        }

        private static string GetStatusEmoji(string status)
        {
            return status switch
            {
                "Bàn trống" => "⚪",
                "Bàn bận" => "🔴",
                "Bàn đã đặt" => "🟡",
                _ => "❓"
            };
        }

        // Cập nhật thống kê
        public void UpdateStatistics(ObservableCollection<Ban> tables)
        {
            if (tables == null) return;

            TotalTablesLabel.Text = tables.Count.ToString();
            EmptyTablesLabel.Text = tables.Count(t => t.TrangThai == "Bàn trống").ToString();
            OccupiedTablesLabel.Text = tables.Count(t => t.TrangThai == "Bàn bận").ToString();
            ReservedTablesLabel.Text = tables.Count(t => t.TrangThai == "Bàn đã đặt").ToString();
        }

        // Event Handlers
        private void OnOverlayTapped(object sender, EventArgs e)
        {
            _ = CloseAsync();
        }

        private void OnChangeStatusTapped(object sender, EventArgs e)
        {
            if (SelectedTable != null)
            {
                ChangeStatusRequested?.Invoke(this, SelectedTable);
            }
        }

        private void OnViewAddOrderTapped(object sender, EventArgs e)
        {
            if (SelectedTable != null)
            {
                ViewAddOrderRequested?.Invoke(this, SelectedTable);
            }
        }

        private void OnPaymentTapped(object sender, EventArgs e)
        {
            if (SelectedTable != null)
            {
                PaymentRequested?.Invoke(this, SelectedTable);
            }
        }

        private void OnRefreshTapped(object sender, EventArgs e)
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnStatusFilterChanged(object sender, EventArgs e)
        {
            var selectedFilter = StatusFilterPicker.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedFilter))
            {
                FilterChanged?.Invoke(this, selectedFilter);
            }
        }
    }
}