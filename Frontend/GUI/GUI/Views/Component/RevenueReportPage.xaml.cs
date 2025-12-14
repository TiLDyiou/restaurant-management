using Microsoft.Maui.Controls.Shapes;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using System.Net.Http.Json;
using System.Text.Json;

namespace RestaurantManagementGUI.Views
{
    public partial class RevenueReportPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private DateTime _startDate, _endDate;
        private string _currentPeriod = "day";
        private bool _isAdmin = false;
        private string _currentMaNV = "";

        public RevenueReportPage()
        {
            InitializeComponent();

            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
            _httpClient = new HttpClient(handler) { BaseAddress = new Uri(ApiConfig.BaseUrl) };
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            _endDate = DateTime.Now;
            _startDate = DateTime.Now.Date;

            // ĐĂNG KÝ SỰ KIỆN
            PaymentEventService.PaymentCompleted += OnPaymentCompleted;

            CheckUserRole();
        }

        private async void OnPaymentCompleted(object sender, PaymentCompletedEventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("💰 Tiền về!", $"Vừa thu {e.TongTien:N0}đ", "OK");
                await LoadRevenueData(); // Reload ngay lập tức
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await Task.Delay(300);
            await LoadRevenueData();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            PaymentEventService.PaymentCompleted -= OnPaymentCompleted;
        }

        private void CheckUserRole()
        {
            // Giả lập lấy role từ storage
            _currentMaNV = UserState.CurrentMaNV ?? "";
            _isAdmin = (UserState.CurrentRole?.ToLower() == "admin");

            if (_isAdmin) { PageTitleLabel.Text = "Doanh Số (Admin)"; RightPanelTitle.Text = "🏆 Hiệu Suất Nhân Viên"; }
            else { PageTitleLabel.Text = "Doanh Số Của Tôi"; RightPanelTitle.Text = "📋 Chi Tiết Giao Dịch"; }
        }

        // --- LOAD DATA TỪ API ---
        private async Task LoadRevenueData()
        {
            try
            {
                string url = $"{ApiConfig.RevenueReport}?startDate={_startDate:yyyy-MM-dd}&endDate={_endDate:yyyy-MM-dd}";
                if (!_isAdmin && !string.IsNullOrEmpty(_currentMaNV)) url += $"&maNV={_currentMaNV}";

                var response = await _httpClient.GetFromJsonAsync<ApiResponse<RevenueReportDto>>(url, _jsonOptions);

                if (response != null && response.Success && response.Data != null)
                    UpdateUI(response.Data);
                else
                    ResetUI();
            }
            catch { ResetUI(); }
        }

        private void UpdateUI(RevenueReportDto data)
        {
            TotalRevenueLabel.Text = $"{data.TotalRevenue:N0} ₫";
            TotalOrdersLabel.Text = data.TotalOrders.ToString();
            AverageOrderLabel.Text = $"{data.AverageOrderValue:N0} ₫";
            RevenueTrendLabel.Text = data.RevenueTrend >= 0 ? $"↑ {data.RevenueTrend:F1}%" : $"↓ {Math.Abs(data.RevenueTrend):F1}%";
            RevenueTrendLabel.TextColor = data.RevenueTrend >= 0 ? Colors.Green : Colors.Red;

            DrawRevenueChart(data.DailyRevenues);

            if (_isAdmin) DrawEmployeeChart(data.TopEmployees);
            else DrawTransactionList(data.RecentTransactions);
        }

        private void ResetUI()
        {
            TotalRevenueLabel.Text = "0 ₫";
            RevenueChartContainer.Children.Clear();
            RightPanelContent.Children.Clear();
        }

        // --- VẼ BIỂU ĐỒ (LOGIC UI) ---
        private void DrawRevenueChart(List<DailyRevenueDto> list)
        {
            RevenueChartContainer.Children.Clear();
            RevenueChartContainer.ColumnDefinitions.Clear();
            if (list == null || list.Count == 0) return;

            decimal max = list.Max(x => x.Revenue);
            if (max == 0) max = 1;

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                RevenueChartContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = 50 });

                var stack = new VerticalStackLayout { Spacing = 5, VerticalOptions = LayoutOptions.End };

                // Cột
                var bar = new BoxView { Color = Color.FromArgb("#FFBD59"), WidthRequest = 40, CornerRadius = new CornerRadius(5, 5, 0, 0) };
                bar.HeightRequest = Math.Max(10, 200 * ((double)item.Revenue / (double)max));

                stack.Children.Add(new Label { Text = FormatCurrency(item.Revenue), FontSize = 10, HorizontalOptions = LayoutOptions.Center });
                stack.Children.Add(bar);
                stack.Children.Add(new Label { Text = item.Date.ToString("dd/MM"), FontSize = 10, HorizontalOptions = LayoutOptions.Center });

                Grid.SetColumn(stack, i);
                Grid.SetRow(stack, 0);
                Grid.SetRowSpan(stack, 3);
                RevenueChartContainer.Children.Add(stack);
            }
        }

        private void DrawEmployeeChart(List<EmployeePerformanceDto> list)
        {
            RightPanelContent.Children.Clear();
            if (list == null) return;

            decimal max = list.Any() ? list.Max(x => x.TotalRevenue) : 1;
            if (max == 0) max = 1;

            foreach (var item in list)
            {
                var grid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = 100 }, new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = 90 } }, Margin = new Thickness(0, 0, 0, 10) };

                grid.Children.Add(new Label { Text = item.EmployeeName, VerticalOptions = LayoutOptions.Center, FontSize = 13, FontAttributes = FontAttributes.Bold });

                var bar = new BoxView { Color = Color.FromArgb("#4CAF50"), HeightRequest = 20, CornerRadius = 5, HorizontalOptions = LayoutOptions.Start, WidthRequest = Math.Max(10, 200 * ((double)item.TotalRevenue / (double)max)) };
                Grid.SetColumn(bar, 1);
                grid.Children.Add(bar);

                var lbl = new Label { Text = $"{item.TotalRevenue:N0}", HorizontalOptions = LayoutOptions.End, FontSize = 12, TextColor = Colors.Green };
                Grid.SetColumn(lbl, 2);
                grid.Children.Add(lbl);

                RightPanelContent.Children.Add(grid);
            }
        }

        private void DrawTransactionList(List<TransactionDetailDto> list)
        {
            RightPanelContent.Children.Clear();
            if (list == null) return;

            foreach (var item in list)
            {
                var frame = new Border { StrokeShape = new RoundRectangle { CornerRadius = 10 }, Padding = 10, Margin = new Thickness(0, 0, 0, 8), BackgroundColor = Colors.White };
                var grid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } } };

                var stack = new VerticalStackLayout();
                stack.Children.Add(new Label { Text = $"#{item.MaHD}", FontAttributes = FontAttributes.Bold });
                stack.Children.Add(new Label { Text = item.ThoiGian.ToString("dd/MM HH:mm"), FontSize = 12, TextColor = Colors.Gray });

                var stackRight = new VerticalStackLayout { HorizontalOptions = LayoutOptions.End };
                stackRight.Children.Add(new Label { Text = $"{item.TongTien:N0}đ", TextColor = Colors.Green, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.End });
                stackRight.Children.Add(new Label { Text = item.TrangThai, FontSize = 11, TextColor = Colors.Blue, HorizontalOptions = LayoutOptions.End });

                grid.Children.Add(stack);
                grid.Children.Add(stackRight); Grid.SetColumn(stackRight, 1);

                frame.Content = grid;
                RightPanelContent.Children.Add(frame);
            }
        }

        private string FormatCurrency(decimal amount) => amount >= 1000000 ? $"{amount / 1000000:F1}M" : $"{amount / 1000:F0}K";

        // Navigation & Filter handlers
        private async void OnBackClicked(object sender, EventArgs e) => await Navigation.PopAsync();
        private async void OnDayClicked(object sender, EventArgs e) => await UpdatePeriod("day", DateTime.Now.Date, DateTime.Now);
        private async void OnWeekClicked(object sender, EventArgs e) => await UpdatePeriod("week", DateTime.Now.AddDays(-7), DateTime.Now);
        private async void OnMonthClicked(object sender, EventArgs e) => await UpdatePeriod("month", new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1), DateTime.Now);
        private async void OnYearClicked(object sender, EventArgs e) => await UpdatePeriod("year", new DateTime(DateTime.Now.Year, 1, 1), DateTime.Now);

        private async Task UpdatePeriod(string period, DateTime start, DateTime end)
        {
            _currentPeriod = period; _startDate = start; _endDate = end;
            DayButton.BackgroundColor = period == "day" ? Color.FromArgb("#FFBD59") : Colors.White;
            WeekButton.BackgroundColor = period == "week" ? Color.FromArgb("#FFBD59") : Colors.White;
            MonthButton.BackgroundColor = period == "month" ? Color.FromArgb("#FFBD59") : Colors.White;
            YearButton.BackgroundColor = period == "year" ? Color.FromArgb("#FFBD59") : Colors.White;
            await LoadRevenueData();
        }
    }
}