using RestaurantManagementGUI.Models;
using System.Collections.ObjectModel;

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
            var table = (Ban)newValue;

            if (table != null)
            {
                control.SelectedTableInfo.IsVisible = true;
                control.QuickActionsSection.IsVisible = true;
                control.SelectedTableName.Text = table.TenBan;
                control.SelectedTableStatus.Text = GetStatusEmoji(table.TrangThai) + " " + table.TrangThai;
            }
            else
            {
                control.SelectedTableInfo.IsVisible = false;
                control.QuickActionsSection.IsVisible = false;
            }
        }

        // Helper: Lấy emoji theo trạng thái
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