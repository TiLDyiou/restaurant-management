using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using System.Diagnostics;

namespace RestaurantManagementGUI.Views
{
    public partial class RevenueReportPage : ContentPage
    {
        private readonly ApiService _apiService;
        private DateTime _startDate;
        private DateTime _endDate;
        private string _currentPeriod = "day";
        private bool _isAdmin = false;
        private string _currentMaNV = "";

        public RevenueReportPage()
        {
            InitializeComponent();
            _apiService = new ApiService();
            _endDate = DateTime.Now;
            _startDate = DateTime.Now.Date;

            // Đăng ký event thanh toán
            PaymentEventService.PaymentCompleted += OnPaymentCompleted;
            Debug.WriteLine("[REVENUE] ✅ Đã đăng ký event PaymentCompleted");

            CheckUserRole();
            Loaded += async (s, e) => await LoadRevenueData();
        }

        private async void OnPaymentCompleted(object sender, PaymentCompletedEventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                Debug.WriteLine($"[REVENUE] 💰 Thanh toán: {e.MaHD} - {e.TongTien:N0} VNĐ");

                await DisplayAlert(
                    "💰 Thanh toán mới!",
                    $"Hóa đơn: {e.MaHD}\nSố tiền: {e.TongTien:N0} VNĐ",
                    "OK"
                );

                await LoadRevenueData();
            });
        }

        private void CheckUserRole()
        {
            _currentMaNV = UserState.CurrentMaNV ?? "";
            _isAdmin = (UserState.CurrentRole?.Trim().ToLower() == "admin");

            Debug.WriteLine($"[REVENUE] Role: {UserState.CurrentRole} → IsAdmin: {_isAdmin}");
            Debug.WriteLine($"[REVENUE] MaNV: {_currentMaNV}");

            if (_isAdmin)
            {
                PageTitleLabel.Text = "Doanh Số";
                RightPanelTitle.Text = "🏆 Hiệu Suất Nhân Viên";
            }
            else
            {
                PageTitleLabel.Text = "Doanh Số Của Tôi";
                RightPanelTitle.Text = "📋 Chi Tiết Giao Dịch";
            }
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

        private void UpdateLabel()
        {
            string periodText = _currentPeriod switch
            {
                "day" => "Hôm nay",
                "week" => "7 ngày gần nhất",
                "month" => $"Tháng {DateTime.Now.Month}/{DateTime.Now.Year}",
                "year" => $"Năm {DateTime.Now.Year}",
                _ => "Hôm nay"
            };
            DateRangeLabel.Text = periodText;
        }

        private async Task LoadRevenueData()
        {
            try
            {
                IsBusy = true;
                Debug.WriteLine($"[REVENUE] 📊 Loading: {_startDate:yyyy-MM-dd} to {_endDate:yyyy-MM-dd}");

                string url = $"orders/revenue-report?startDate={_startDate:yyyy-MM-dd}&endDate={_endDate:yyyy-MM-dd}";

                if (!_isAdmin && !string.IsNullOrEmpty(_currentMaNV))
                {
                    url += $"&maNV={_currentMaNV}";
                    Debug.WriteLine($"[REVENUE] Staff mode: {_currentMaNV}");
                }

                var response = await _apiService.GetAsync<ApiResponse<RevenueReportData>>(url);

                if (response?.Success == true && response.Data != null)
                {
                    Debug.WriteLine($"[REVENUE] ✅ Loaded: {response.Data.TotalRevenue:N0} VNĐ, {response.Data.TotalOrders} orders");
                    Debug.WriteLine($"[REVENUE] Employees: {response.Data.TopEmployees?.Count ?? 0}");
                    Debug.WriteLine($"[REVENUE] Transactions: {response.Data.RecentTransactions?.Count ?? 0}");

                    UpdateUI(response.Data);
                }
                else
                {
                    Debug.WriteLine("[REVENUE] ⚠️ No data");
                    ResetUI();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[REVENUE] ❌ Error: {ex.Message}");
                await DisplayAlert("Lỗi", $"Không thể tải dữ liệu: {ex.Message}", "OK");
                ResetUI();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateUI(RevenueReportData data)
        {
            // Update summary cards
            TotalRevenueLabel.Text = $"{data.TotalRevenue:N0} ₫";
            TotalOrdersLabel.Text = data.TotalOrders.ToString();
            AverageOrderLabel.Text = data.TotalOrders > 0 ? $"{data.AverageOrderValue:N0} ₫" : "0 ₫";

            if (data.RevenueTrend != 0)
            {
                RevenueTrendLabel.Text = $"{(data.RevenueTrend >= 0 ? "↑" : "↓")} {Math.Abs(data.RevenueTrend):F1}%";
                RevenueTrendLabel.TextColor = data.RevenueTrend >= 0 ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
            }
            else
            {
                RevenueTrendLabel.Text = "--";
            }

            // Draw revenue chart
            DrawRevenueChart(data.DailyRevenues);

            // Draw right panel based on role
            if (_isAdmin)
            {
                Debug.WriteLine("[REVENUE] Drawing employee chart (ADMIN)");
                DrawEmployeeChart(data.TopEmployees);
            }
            else
            {
                Debug.WriteLine("[REVENUE] Drawing transaction list (STAFF)");
                DrawTransactionList(data.RecentTransactions);
            }
        }

        private void DrawRevenueChart(List<DailyRevenue> dailyRevenues)
        {
            RevenueChartContainer.Children.Clear();
            RevenueChartContainer.ColumnDefinitions.Clear();

            if (dailyRevenues == null || dailyRevenues.Count == 0)
            {
                EmptyChartState.IsVisible = true;
                if (!RevenueChartContainer.Children.Contains(EmptyChartState))
                {
                    RevenueChartContainer.Children.Add(EmptyChartState);
                    Grid.SetRowSpan(EmptyChartState, 3);
                }
                return;
            }

            EmptyChartState.IsVisible = false;
            var max = dailyRevenues.Max(x => x.Revenue);
            if (max == 0) max = 1;

            int barWidth = 50;
            RevenueChartContainer.MinimumWidthRequest = Math.Max(600, (barWidth + 10) * dailyRevenues.Count);

            for (int i = 0; i < dailyRevenues.Count; i++)
            {
                var item = dailyRevenues[i];
                RevenueChartContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = barWidth });

                var stack = new VerticalStackLayout { Spacing = 5, VerticalOptions = LayoutOptions.End };

                stack.Children.Add(new Label
                {
                    Text = FormatCurrency(item.Revenue),
                    FontSize = 10,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#263238"),
                    HorizontalOptions = LayoutOptions.Center
                });

                stack.Children.Add(new BoxView
                {
                    Color = Color.FromArgb("#FFBD59"),
                    HeightRequest = Math.Max(10, 200 * ((double)item.Revenue / (double)max)),
                    WidthRequest = 40,
                    CornerRadius = new CornerRadius(5, 5, 0, 0),
                    HorizontalOptions = LayoutOptions.Center
                });

                stack.Children.Add(new Label
                {
                    Text = FormatDate(item.Date),
                    FontSize = 10,
                    TextColor = Color.FromArgb("#666"),
                    HorizontalOptions = LayoutOptions.Center
                });

                Grid.SetColumn(stack, i);
                Grid.SetRow(stack, 0);
                Grid.SetRowSpan(stack, 3);
                RevenueChartContainer.Children.Add(stack);
            }
        }

        private void DrawEmployeeChart(List<EmployeePerformance> employees)
        {
            RightPanelContent.Children.Clear();

            if (employees == null || employees.Count == 0)
            {
                EmptyRightPanelState.IsVisible = true;
                return;
            }

            EmptyRightPanelState.IsVisible = false;
            var maxRevenue = employees.Max(e => e.TotalRevenue);
            if (maxRevenue == 0) maxRevenue = 1;

            Debug.WriteLine($"[REVENUE] ⭐ Drawing {employees.Count} employees");

            foreach (var emp in employees)
            {
                double widthPercent = (double)emp.TotalRevenue / (double)maxRevenue;

                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = 120 },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = 100 }
                    },
                    RowDefinitions =
                    {
                        new RowDefinition { Height = GridLength.Auto }
                    },
                    Margin = new Thickness(0, 0, 0, 15)
                };

                // Employee name
                grid.Children.Add(new Label
                {
                    Text = emp.EmployeeName,
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Color.FromArgb("#263238")
                });

                // Progress bar container
                var barBackground = new Border
                {
                    BackgroundColor = Color.FromArgb("#F0F0F0"),
                    HeightRequest = 28,
                    Margin = new Thickness(5, 0, 5, 0),
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 6 }
                };

                var bar = new BoxView
                {
                    Color = Color.FromArgb("#4CAF50"),
                    CornerRadius = new CornerRadius(6),
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Fill,
                    WidthRequest = Math.Max(40, 250 * widthPercent)
                };

                barBackground.Content = new Grid { Children = { bar } };
                Grid.SetColumn(barBackground, 1);
                grid.Children.Add(barBackground);

                // Revenue amount
                var revenueLabel = new Label
                {
                    Text = $"{emp.TotalRevenue:N0} ₫",
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#4CAF50"),
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(revenueLabel, 2);
                grid.Children.Add(revenueLabel);

                RightPanelContent.Children.Add(grid);
            }
        }

        private void DrawTransactionList(List<TransactionDetail> transactions)
        {
            RightPanelContent.Children.Clear();

            if (transactions == null || transactions.Count == 0)
            {
                EmptyRightPanelState.IsVisible = true;
                return;
            }

            EmptyRightPanelState.IsVisible = false;

            Debug.WriteLine($"[REVENUE] 📋 Drawing {transactions.Count} transactions");

            foreach (var trans in transactions)
            {
                var border = new Border
                {
                    Stroke = Color.FromArgb("#E0E0E0"),
                    StrokeThickness = 1,
                    BackgroundColor = Colors.White,
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 10),
                    StrokeShape = new RoundRectangle { CornerRadius = 10 }
                };

                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    RowDefinitions =
                    {
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = GridLength.Auto }
                    },
                    RowSpacing = 8
                };

                // Order ID
                var orderIdLabel = new Label
                {
                    Text = $"#{trans.MaHD}",
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#263238")
                };
                Grid.SetRow(orderIdLabel, 0);
                Grid.SetColumn(orderIdLabel, 0);
                grid.Children.Add(orderIdLabel);

                // Amount
                var amountLabel = new Label
                {
                    Text = $"{trans.TongTien:N0} ₫",
                    FontSize = 17,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#4CAF50"),
                    HorizontalOptions = LayoutOptions.End
                };
                Grid.SetRow(amountLabel, 0);
                Grid.SetColumn(amountLabel, 1);
                grid.Children.Add(amountLabel);

                // Date/Time
                var dateLabel = new Label
                {
                    Text = trans.ThoiGian.ToString("dd/MM/yyyy HH:mm"),
                    FontSize = 12,
                    TextColor = Color.FromArgb("#666")
                };
                Grid.SetRow(dateLabel, 1);
                Grid.SetColumn(dateLabel, 0);
                grid.Children.Add(dateLabel);

                // Status
                var statusLabel = new Label
                {
                    Text = trans.TrangThai,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#2196F3"),
                    HorizontalOptions = LayoutOptions.End
                };
                Grid.SetRow(statusLabel, 1);
                Grid.SetColumn(statusLabel, 1);
                grid.Children.Add(statusLabel);

                border.Content = grid;
                RightPanelContent.Children.Add(border);
            }
        }

        private void ResetUI()
        {
            TotalRevenueLabel.Text = "0 ₫";
            TotalOrdersLabel.Text = "0";
            AverageOrderLabel.Text = "0 ₫";
            RevenueTrendLabel.Text = "--";

            RevenueChartContainer.Children.Clear();
            EmptyChartState.IsVisible = true;
            if (!RevenueChartContainer.Children.Contains(EmptyChartState))
            {
                RevenueChartContainer.Children.Add(EmptyChartState);
            }

            RightPanelContent.Children.Clear();
            EmptyRightPanelState.IsVisible = true;
        }

        private string FormatCurrency(decimal amount)
        {
            if (amount >= 1000000) return $"{amount / 1000000:F1}M";
            if (amount >= 1000) return $"{amount / 1000:F0}K";
            return $"{amount:F0}";
        }

        private string FormatDate(DateTime date)
        {
            return _currentPeriod switch
            {
                "day" => date.ToString("HH:mm"),
                "week" => date.ToString("dd/MM"),
                "month" => date.ToString("dd/MM"),
                "year" => date.ToString("MM/yyyy"),
                _ => date.ToString("dd/MM")
            };
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            PaymentEventService.PaymentCompleted -= OnPaymentCompleted;
            Debug.WriteLine("[REVENUE] ✅ Unsubscribed event");
        }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}