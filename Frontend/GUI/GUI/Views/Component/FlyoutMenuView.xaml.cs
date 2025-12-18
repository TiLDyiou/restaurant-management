using RestaurantManagementGUI.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using RestaurantManagementGUI.Constants;

namespace RestaurantManagementGUI
{
    public partial class FlyoutMenuView : ContentView
    {
        public static readonly BindableProperty SelectedTableProperty =
            BindableProperty.Create(nameof(SelectedTable), typeof(Ban), typeof(FlyoutMenuView), null, propertyChanged: OnSelectedTableChanged);

        public Ban SelectedTable
        {
            get => (Ban)GetValue(SelectedTableProperty);
            set => SetValue(SelectedTableProperty, value);
        }

        public event EventHandler<Ban> ChangeStatusRequested;
        public event EventHandler<Ban> ViewAddOrderRequested;
        public event EventHandler<Ban> PaymentRequested;
        public event EventHandler RefreshRequested;
        public event EventHandler<string> FilterChanged;

        public FlyoutMenuView()
        {
            InitializeComponent();
            StatusFilterPicker.SelectedIndex = 0;
        }

        public async Task OpenAsync()
        {
            FlyoutContainer.IsVisible = true;
            FlyoutContainer.InputTransparent = false;
            await Task.WhenAll(Overlay.FadeTo(1, 250), FlyoutPanel.TranslateTo(0, 0, 300, Easing.CubicOut));
        }

        public async Task CloseAsync()
        {
            await Task.WhenAll(FlyoutPanel.TranslateTo(-320, 0, 250), Overlay.FadeTo(0, 250));
            FlyoutContainer.IsVisible = false;
            FlyoutContainer.InputTransparent = true;
        }

        private static void OnSelectedTableChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (FlyoutMenuView)bindable;
            if (oldValue is Ban oldTable) oldTable.PropertyChanged -= control.OnTablePropertyChanged;

            if (newValue is Ban newTable)
            {
                newTable.PropertyChanged += control.OnTablePropertyChanged;
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

        private void OnTablePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Ban.TrangThai))
                MainThread.BeginInvokeOnMainThread(() => UpdateFlyoutUI(SelectedTable));
        }

        private void UpdateFlyoutUI(Ban table)
        {
            if (table == null) return;
            SelectedTableName.Text = table.TenBan;

            string status = table.TrangThai;
            if (status == SystemConstants.TableEmpty)
            {
                SelectedTableStatus.Text = "Trống";
                SelectedTableStatus.TextColor = Colors.Green;
            }
            else if (status == SystemConstants.TableOccupied)
            {
                SelectedTableStatus.Text = "Có khách";
                SelectedTableStatus.TextColor = Colors.Red;
            }
            else
            {
                SelectedTableStatus.Text = "Đã đặt";
                SelectedTableStatus.TextColor = Color.FromArgb("#FFBD59");
            }
        }

        public void UpdateStatistics(ObservableCollection<Ban> tables)
        {
            if (tables == null) return;
            TotalTablesLabel.Text = tables.Count.ToString();
            EmptyTablesLabel.Text = tables.Count(t => t.TrangThai == SystemConstants.TableEmpty).ToString();
            OccupiedTablesLabel.Text = tables.Count(t => t.TrangThai == SystemConstants.TableOccupied).ToString();
            ReservedTablesLabel.Text = tables.Count(t => t.TrangThai == SystemConstants.TableReserved).ToString();
        }

        private void OnOverlayTapped(object sender, EventArgs e) => _ = CloseAsync();
        private void OnChangeStatusTapped(object sender, EventArgs e) => ChangeStatusRequested?.Invoke(this, SelectedTable);
        private void OnViewAddOrderTapped(object sender, EventArgs e) => ViewAddOrderRequested?.Invoke(this, SelectedTable);
        private void OnPaymentTapped(object sender, EventArgs e) => PaymentRequested?.Invoke(this, SelectedTable);
        private void OnStatusFilterChanged(object sender, EventArgs e) => FilterChanged?.Invoke(this, StatusFilterPicker.SelectedItem?.ToString());
    }
}