using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using RestaurantManagementGUI.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;

namespace RestaurantManagementGUI.Views
{
    public partial class RevenueReportPage : ContentPage
    {
        private readonly ApiService _apiService;
        private DateTime _startDate;
        private DateTime _endDate;
        private string _currentPeriod = "day";

        public RevenueReportPage()
        {
            InitializeComponent();
            _apiService = new ApiService();
            _endDate = DateTime.Now;
            _startDate = DateTime.Now.Date;
            Loaded += async (s, e) => await LoadRevenueData();
        }

        private async void OnBackClicked(object sender, EventArgs e) => await Navigation.PopAsync();

        private async void OnDayClicked(object sender, EventArgs e) => await UpdatePeriod("day", DateTime.Now.Date, DateTime.Now);
        private async void OnWeekClicked(object sender, EventArgs e) => await UpdatePeriod("week", DateTime.Now.AddDays(-7), DateTime.Now);
        private async void OnMonthClicked(object sender, EventArgs e) => await UpdatePeriod("month", new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1), DateTime.Now);
        private async void OnYearClicked(object sender, EventArgs e) => await UpdatePeriod("year", new DateTime(DateTime.Now.Year, 1, 1), DateTime.Now);

        private async Task UpdatePeriod(string period, DateTime start, DateTime end)
        {
            SetActiveButton(period);
            _startDate = start;
            _endDate = end;
            UpdateLabel();
            await LoadRevenueData();
        }

        private void SetActiveButton(string period)
        {
            _currentPeriod = period;
            DayButton.BackgroundColor = WeekButton.BackgroundColor = MonthButton.BackgroundColor = YearButton.BackgroundColor = Colors.White;
            DayButton.TextColor = WeekButton.TextColor = MonthButton.TextColor = YearButton.TextColor = Color.FromArgb("#666");

            var btn = period switch { "week" => WeekButton, "month" => MonthButton, "year" => YearButton, _ => DayButton };
            btn.BackgroundColor = Color.FromArgb("#FFBD59");
            btn.TextColor = Color.FromArgb("#263238");
        }

        private void UpdateLabel() => DateRangeLabel.Text = $"Toàn hệ thống - {_startDate:dd/MM} đến {_endDate:dd/MM/yyyy}";

        private async Task LoadRevenueData()
        {
            try
            {
                IsBusy = true;
                string url = $"orders/revenue-report?startDate={_startDate:yyyy-MM-dd}&endDate={_endDate:yyyy-MM-dd}";

                // Gọi API
                var response = await _apiService.GetAsync<ApiResponse<RevenueReportData>>(url);

                if (response != null && response.Success && response.Data != null)
                {
                    UpdateUI(response.Data);
                }
                else
                {
                    ResetUI();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", "Không thể kết nối Server", "OK");
                ResetUI();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateUI(RevenueReportData data)
        {
            TotalRevenueLabel.Text = $"{data.TotalRevenue:N0} ₫";
            TotalOrdersLabel.Text = data.TotalOrders.ToString();
            AverageOrderLabel.Text = $"{data.AverageOrderValue:N0} ₫";

            // Vẽ biểu đồ
            RevenueChartContainer.Children.Clear();
            if (data.DailyRevenues.Count > 0) EmptyChartState.IsVisible = false;

            var max = data.DailyRevenues.Max(x => x.Revenue);
            if (max == 0) max = 1;

            for (int i = 0; i < data.DailyRevenues.Count; i++)
            {
                var item = data.DailyRevenues[i];
                RevenueChartContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = 50 });

                var sl = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.End };
                sl.Children.Add(new Label { Text = FormatCurrency(item.Revenue), FontSize = 9, HorizontalOptions = LayoutOptions.Center });
                sl.Children.Add(new BoxView { Color = Color.FromArgb("#FFBD59"), HeightRequest = Math.Max(10, 150 * ((double)item.Revenue / (double)max)), WidthRequest = 40, CornerRadius = new CornerRadius(5, 5, 0, 0) });
                sl.Children.Add(new Label { Text = item.Date.ToString("dd/MM"), FontSize = 10, HorizontalOptions = LayoutOptions.Center });

                Grid.SetColumn(sl, i);
                RevenueChartContainer.Children.Add(sl);
            }

            // Top NV
            EmployeeChartContainer.Children.Clear();
            if (data.TopEmployees.Count > 0) EmptyEmployeeState.IsVisible = false;

            var maxNv = data.TopEmployees.Max(x => x.TotalRevenue);
            if (maxNv == 0) maxNv = 1;

            foreach (var nv in data.TopEmployees)
            {
                var g = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = 100 }, new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = 80 } }, Margin = new Thickness(0, 0, 0, 10) };
                g.Children.Add(new Label { Text = nv.EmployeeName, FontSize = 12, VerticalOptions = LayoutOptions.Center });
                var bar = new BoxView { Color = Color.FromArgb("#4CAF50"), HeightRequest = 20, CornerRadius = 5, HorizontalOptions = LayoutOptions.Start, WidthRequest = Math.Max(20, 200 * ((double)nv.TotalRevenue / (double)maxNv)) };
                Grid.SetColumn(bar, 1); g.Children.Add(bar);
                var money = new Label { Text = $"{nv.TotalRevenue:N0}", FontSize = 12, HorizontalOptions = LayoutOptions.End, FontAttributes = FontAttributes.Bold };
                Grid.SetColumn(money, 2); g.Children.Add(money);
                EmployeeChartContainer.Children.Add(g);
            }
        }

        private void ResetUI()
        {
            TotalRevenueLabel.Text = "0 ₫";
            TotalOrdersLabel.Text = "0";
            RevenueChartContainer.Children.Clear();
            EmployeeChartContainer.Children.Clear();
            EmptyChartState.IsVisible = true;
            EmptyEmployeeState.IsVisible = true;
        }

        private string FormatCurrency(decimal amount)
        {
            if (amount >= 1000000) return $"{amount / 1000000:F1}M";
            if (amount >= 1000) return $"{amount / 1000:F0}K";
            return $"{amount:F0}";
        }
    }

    // === MODEL NẰM TẠI ĐÂY ĐỂ TRÁNH LỖI ===
    public class RevenueReportData
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal RevenueTrend { get; set; }
        public List<DailyRevenue> DailyRevenues { get; set; } = new();
        public List<EmployeePerformance> TopEmployees { get; set; } = new();
    }
    public class DailyRevenue
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }
    public class EmployeePerformance
    {
        public string EmployeeName { get; set; }
        public int OrdersServed { get; set; }
        public decimal TotalRevenue { get; set; }
    }
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}