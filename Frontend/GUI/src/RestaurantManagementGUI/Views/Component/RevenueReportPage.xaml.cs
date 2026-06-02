using Microsoft.Maui.Controls.Shapes;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;

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
        private RevenueReportDto _currentData;

        public RevenueReportPage(HttpClient httpClient)
        {
            InitializeComponent();

            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            _endDate = DateTime.Now;
            _startDate = DateTime.Now.Date;
            PaymentEventService.PaymentCompleted += OnPaymentCompleted;
            CheckUserRole();
        }
        private async void OnPaymentCompleted(object sender, PaymentCompletedEventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("💰 Tiền về!", $"Vừa thu {e.TongTien:N0}đ", "OK");
                await LoadRevenueData();
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
            _currentMaNV = UserState.CurrentMaNV ?? "";
            _isAdmin = (UserState.CurrentRole?.ToLower() == "admin");

            if (_isAdmin) { PageTitleLabel.Text = "Doanh Số"; RightPanelTitle.Text = "🏆 Hiệu Suất Món Ăn"; }
            else { PageTitleLabel.Text = "Doanh Số Của Tôi"; RightPanelTitle.Text = "📋 Chi Tiết Giao Dịch"; }
        }

        private async Task LoadRevenueData()
        {
            try
            {
                string groupBy = _currentPeriod == "year" ? "month" : "day";
                string url = $"{ApiConfig.RevenueReport}?startDate={_startDate:yyyy-MM-dd}&endDate={_endDate:yyyy-MM-dd}&groupBy={groupBy}";

                if (!_isAdmin && !string.IsNullOrEmpty(_currentMaNV))
                    url += $"&maNV={_currentMaNV}";

                var response = await _httpClient.GetFromJsonAsync<ApiResponse<RevenueReportDto>>(url, _jsonOptions);

                if (response != null && response.Success && response.Data != null)
                {
                    _currentData = response.Data;
                    UpdateUI(response.Data);
                }
                else
                {
                    _currentData = null;
                    ResetUI();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ResetUI();
            }
        }

        private void UpdateUI(RevenueReportDto data)
        {
            TotalRevenueLabel.Text = $"{data.TotalRevenue:N0} ₫";
            TotalOrdersLabel.Text = data.TotalOrders.ToString();
            AverageOrderLabel.Text = $"{data.AverageOrderValue:N0} ₫";
            RevenueTrendLabel.Text = data.RevenueTrend >= 0 ? $"↑ {data.RevenueTrend:F1}%" : $"↓ {Math.Abs(data.RevenueTrend):F1}%";
            RevenueTrendLabel.TextColor = data.RevenueTrend >= 0 ? Colors.Green : Colors.Red;

            DrawRevenueChart(data.DailyRevenues);

            if (_isAdmin) DrawDishesChart(data.TopDishes, data.BottomDishes);
            else DrawTransactionList(data.RecentTransactions);
        }

        private void ResetUI()
        {
            TotalRevenueLabel.Text = "0 ₫";
            RevenueChartContainer.Children.Clear();
            RightPanelContent.Children.Clear();
        }
        private void DrawRevenueChart(List<DailyRevenueDto> list)
        {
            RevenueChartContainer.Children.Clear();
            RevenueChartContainer.ColumnDefinitions.Clear();

            // Nếu list null hoặc rỗng thì hiện trạng thái Empty
            if (list == null || list.Count == 0)
            {
                EmptyChartState.IsVisible = true;
                return;
            }

            EmptyChartState.IsVisible = false; // Ẩn icon "Không có dữ liệu"

            decimal max = list.Max(x => x.Revenue);
            if (max == 0) max = 1;

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];

                // Điều chỉnh độ rộng cột tùy theo số lượng dữ liệu để nhìn đẹp hơn
                // Nếu xem năm (12 tháng) thì cột to hơn chút, xem tháng (30 ngày) thì cột nhỏ lại
                double colWidth = _currentPeriod == "year" ? 60 : 50;
                RevenueChartContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = colWidth });

                var stack = new VerticalStackLayout { Spacing = 5, VerticalOptions = LayoutOptions.End };

                bool isCurrent = _currentPeriod == "year"
                    ? (item.Date.Month == DateTime.Now.Month && item.Date.Year == DateTime.Now.Year)
                    : (item.Date.Date == DateTime.Now.Date);

                Color barColor = isCurrent ? Color.FromArgb("#FF9800") : Color.FromArgb("#FFBD59");

                var bar = new BoxView { Color = barColor, WidthRequest = colWidth - 10, CornerRadius = new CornerRadius(5, 5, 0, 0) };

                // Tính chiều cao cột (Max height 200)
                bar.HeightRequest = Math.Max(5, 200 * ((double)item.Revenue / (double)max));

                // Label tiền trên đầu cột
                stack.Children.Add(new Label { Text = FormatCurrency(item.Revenue), FontSize = 10, HorizontalOptions = LayoutOptions.Center, TextColor = Colors.Gray });

                stack.Children.Add(bar);

                // Format nhãn thời gian dưới chân cột
                string timeLabel;
                if (_currentPeriod == "year")
                {
                    // Nếu xem năm -> Hiển thị "T1", "T2"...
                    timeLabel = $"T{item.Date.Month}";
                }
                else
                {
                    // Nếu xem ngày/tuần/tháng -> Hiển thị "15/12"
                    timeLabel = item.Date.ToString("dd/MM");
                }

                stack.Children.Add(new Label { Text = timeLabel, FontSize = 11, HorizontalOptions = LayoutOptions.Center, FontAttributes = isCurrent ? FontAttributes.Bold : FontAttributes.None });

                Grid.SetColumn(stack, i);
                Grid.SetRow(stack, 0);
                Grid.SetRowSpan(stack, 3);
                RevenueChartContainer.Children.Add(stack);
            }
        }

        private void DrawDishesChart(List<DishPerformanceDto> top, List<DishPerformanceDto> bottom)
        {
            RightPanelContent.Children.Clear();
            
            if (top != null && top.Any())
            {
                RightPanelContent.Children.Add(new Label { Text = "Bán chạy nhất", FontAttributes = FontAttributes.Bold, TextColor = Colors.Green, Margin = new Thickness(0, 5) });
                decimal max = top.Max(x => x.Revenue);
                if (max == 0) max = 1;

                foreach (var item in top)
                {
                    var grid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = 110 }, new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = 90 } }, Margin = new Thickness(0, 0, 0, 10) };
                    grid.Children.Add(new Label { Text = item.DishName, VerticalOptions = LayoutOptions.Center, FontSize = 12, FontAttributes = FontAttributes.Bold });
                    
                    var bar = new BoxView { Color = Color.FromArgb("#4CAF50"), HeightRequest = 15, CornerRadius = 5, HorizontalOptions = LayoutOptions.Start, WidthRequest = Math.Max(10, 150 * ((double)item.Revenue / (double)max)) };
                    Grid.SetColumn(bar, 1);
                    grid.Children.Add(bar);

                    var lbl = new Label { Text = $"{item.Revenue:N0}", HorizontalOptions = LayoutOptions.End, FontSize = 11, TextColor = Colors.Green };
                    Grid.SetColumn(lbl, 2);
                    grid.Children.Add(lbl);

                    RightPanelContent.Children.Add(grid);
                }
            }

            if (bottom != null && bottom.Any())
            {
                RightPanelContent.Children.Add(new Label { Text = "Bán chậm nhất", FontAttributes = FontAttributes.Bold, TextColor = Colors.Red, Margin = new Thickness(0, 15, 0, 5) });
                decimal max = bottom.Max(x => x.Revenue);
                if (max == 0) max = 1;

                foreach (var item in bottom)
                {
                    var grid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = 110 }, new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = 90 } }, Margin = new Thickness(0, 0, 0, 10) };
                    grid.Children.Add(new Label { Text = item.DishName, VerticalOptions = LayoutOptions.Center, FontSize = 12, FontAttributes = FontAttributes.Bold });
                    
                    var bar = new BoxView { Color = Color.FromArgb("#F44336"), HeightRequest = 15, CornerRadius = 5, HorizontalOptions = LayoutOptions.Start, WidthRequest = Math.Max(10, 150 * ((double)item.Revenue / (double)max)) };
                    Grid.SetColumn(bar, 1);
                    grid.Children.Add(bar);

                    var lbl = new Label { Text = $"{item.Revenue:N0}", HorizontalOptions = LayoutOptions.End, FontSize = 11, TextColor = Colors.Red };
                    Grid.SetColumn(lbl, 2);
                    grid.Children.Add(lbl);

                    RightPanelContent.Children.Add(grid);
                }
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
        private async void OnBackClicked(object sender, EventArgs e) => await Navigation.PopAsync();
        private async void OnDayClicked(object sender, EventArgs e) => await UpdatePeriod("day", DateTime.Now.Date, DateTime.Now);
        private async void OnWeekClicked(object sender, EventArgs e)
        {
            var today = DateTime.Now;

            // Tính toán độ lệch để tìm về Thứ 2
            // Nếu hôm nay là Thứ 2 (1) -> trừ 0 ngày
            // Nếu hôm nay là Thứ 3 (2) -> trừ 1 ngày
            // Nếu hôm nay là Chủ Nhật (0) -> trừ 6 ngày
            int diff = today.DayOfWeek - DayOfWeek.Monday;
            if (diff < 0)
            {
                diff += 7;
            }

            var startOfWeek = today.AddDays(-diff).Date; // Đây là ngày Thứ 2 đầu tuần
            var endOfWeek = startOfWeek.AddDays(6).Date; // Đây là ngày Chủ Nhật cuối tuần

            // Gọi hàm cập nhật. 
            // Backend của bạn đã có logic tự động lấy hết ngày cuối cùng (23:59:59) 
            // nên ở đây chỉ cần truyền ngày Chủ Nhật là đủ.
            await UpdatePeriod("week", startOfWeek, endOfWeek);
        }
        private async void OnMonthClicked(object sender, EventArgs e) => await UpdatePeriod("month", new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1), DateTime.Now);
        private async void OnYearClicked(object sender, EventArgs e)
        {
            var start = new DateTime(DateTime.Now.Year, 1, 1);
            var end = new DateTime(DateTime.Now.Year, 12, 31);
            await UpdatePeriod("year", start, end);
        }

        private async void OnExportExcelClicked(object sender, EventArgs e)
        {
            if (_currentData == null)
            {
                await DisplayAlert("Lỗi", "Không có dữ liệu để xuất", "OK");
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add("BaoCaoDoanhThu");

                sheet.Cell(1, 1).Value = "BÁO CÁO DOANH THU";
                sheet.Cell(1, 1).Style.Font.Bold = true;
                sheet.Cell(1, 1).Style.Font.FontSize = 16;
                sheet.Range(1, 1, 1, 4).Merge();

                sheet.Cell(3, 1).Value = "Thời gian:";
                sheet.Cell(3, 2).Value = $"{_startDate:dd/MM/yyyy} - {_endDate:dd/MM/yyyy}";

                sheet.Cell(4, 1).Value = "Tổng doanh thu:";
                sheet.Cell(4, 2).Value = _currentData.TotalRevenue;
                sheet.Cell(4, 2).Style.NumberFormat.Format = "#,##0";

                sheet.Cell(5, 1).Value = "Tổng số đơn hàng:";
                sheet.Cell(5, 2).Value = _currentData.TotalOrders;

                sheet.Cell(7, 1).Value = "Món ăn bán chạy / bán chậm";
                sheet.Cell(7, 1).Style.Font.Bold = true;
                
                sheet.Cell(8, 1).Value = "Tên món";
                sheet.Cell(8, 2).Value = "Số lượng";
                sheet.Cell(8, 3).Value = "Doanh thu";
                sheet.Range(8, 1, 8, 3).Style.Font.Bold = true;
                
                int row = 9;
                if (_currentData.TopDishes != null)
                {
                    sheet.Cell(row, 1).Value = "[Bán chạy]";
                    sheet.Cell(row, 1).Style.Font.Italic = true;
                    row++;
                    foreach (var d in _currentData.TopDishes)
                    {
                        sheet.Cell(row, 1).Value = d.DishName;
                        sheet.Cell(row, 2).Value = d.QuantitySold;
                        sheet.Cell(row, 3).Value = d.Revenue;
                        sheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                        row++;
                    }
                }
                
                if (_currentData.BottomDishes != null)
                {
                    sheet.Cell(row, 1).Value = "[Bán chậm]";
                    sheet.Cell(row, 1).Style.Font.Italic = true;
                    row++;
                    foreach (var d in _currentData.BottomDishes)
                    {
                        sheet.Cell(row, 1).Value = d.DishName;
                        sheet.Cell(row, 2).Value = d.QuantitySold;
                        sheet.Cell(row, 3).Value = d.Revenue;
                        sheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                        row++;
                    }
                }

                row++;
                sheet.Cell(row, 1).Value = "CHI TIẾT GIAO DỊCH";
                sheet.Cell(row, 1).Style.Font.Bold = true;
                row++;
                
                sheet.Cell(row, 1).Value = "Mã Hóa Đơn";
                sheet.Cell(row, 2).Value = "Thời gian";
                sheet.Cell(row, 3).Value = "Tổng tiền";
                sheet.Cell(row, 4).Value = "Trạng thái";
                sheet.Range(row, 1, row, 4).Style.Font.Bold = true;
                row++;
                if (_currentData.RecentTransactions != null)
                {
                    foreach (var t in _currentData.RecentTransactions)
                    {
                        sheet.Cell(row, 1).Value = t.MaHD;
                        sheet.Cell(row, 2).Value = t.ThoiGian.ToString("dd/MM/yyyy HH:mm");
                        sheet.Cell(row, 3).Value = t.TongTien;
                        sheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                        sheet.Cell(row, 4).Value = t.TrangThai;
                        row++;
                    }
                }

                sheet.Columns().AdjustToContents();

                string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string filePath = System.IO.Path.Combine(docsPath, $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                
                workbook.SaveAs(filePath);
                await DisplayAlert("Thành công", $"File Excel đã được lưu tại:\n{filePath}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", "Lỗi xuất file Excel: " + ex.Message, "OK");
            }
        }

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