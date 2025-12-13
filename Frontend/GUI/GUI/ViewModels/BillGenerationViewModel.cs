using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.Views; // Để điều hướng báo cáo

namespace RestaurantManagementGUI.ViewModels
{
    public partial class BillGenerationViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;

        // Cấu hình VietQR (Bạn có thể đưa vào file Config sau này)
        private const string BANK_ID = "MB"; // Ví dụ: MB, VCB, TCB...
        private const string ACCOUNT_NO = "0987654321"; // Số tài khoản nhận tiền
        private const string TEMPLATE = "compact";

        // Danh sách hóa đơn chờ
        public ObservableCollection<HoaDonModel> PendingBills { get; } = new();

        // Hóa đơn đang chọn
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(QrCodeUrl))]
        [NotifyPropertyChangedFor(nameof(ChangeAmount))]
        [NotifyPropertyChangedFor(nameof(CanPay))]
        private HoaDonModel _selectedBill;

        // Logic thanh toán
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsTransferPayment))]
        private bool _isCashPayment = true;

        public bool IsTransferPayment => !IsCashPayment;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ChangeAmount))]
        [NotifyPropertyChangedFor(nameof(CanPay))]
        private string _customerPayAmount;

        // Tính tiền thừa (Computed Property)
        public decimal ChangeAmount
        {
            get
            {
                if (SelectedBill == null || string.IsNullOrEmpty(CustomerPayAmount)) return 0;
                if (decimal.TryParse(CustomerPayAmount, out decimal pay))
                {
                    return pay - SelectedBill.TongTien;
                }
                return 0;
            }
        }

        // Link QR động
        public string QrCodeUrl
        {
            get
            {
                if (SelectedBill == null) return "";
                // Tạo link VietQR QuickLink
                var desc = $"Thanh toan HD {SelectedBill.MaHD}";
                return $"https://img.vietqr.io/image/{BANK_ID}-{ACCOUNT_NO}-{TEMPLATE}.png?amount={SelectedBill.TongTien}&addInfo={desc}";
            }
        }

        // Điều kiện để nút Thanh toán sáng lên
        public bool CanPay
        {
            get
            {
                if (SelectedBill == null) return false;
                if (IsCashPayment)
                {
                    // Nếu tiền mặt: Phải nhập đủ tiền
                    if (decimal.TryParse(CustomerPayAmount, out decimal pay))
                        return pay >= SelectedBill.TongTien;
                    return false;
                }
                // Nếu chuyển khoản: Luôn cho phép (giả định đã quét xong)
                return true;
            }
        }

        public BillGenerationViewModel(ApiService apiService)
        {
            _apiService = apiService;
            // Load dữ liệu ngay khi khởi tạo (hoặc gọi từ OnAppearing)
            LoadPendingBillsCommand.Execute(null);
        }

        [RelayCommand]
        public async Task LoadPendingBillsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            var response = await _apiService.GetAsync<List<HoaDonModel>>(ApiConfig.Orders);
            if (response.Success && response.Data != null)
            {
                PendingBills.Clear();
                // Chỉ lấy các đơn chưa thanh toán
                var pending = response.Data
                    .Where(b => b.TrangThai != "Đã thanh toán")
                    .OrderByDescending(b => b.NgayLap);

                foreach (var bill in pending) PendingBills.Add(bill);

                // Tự chọn đơn đầu tiên nếu có
                if (SelectedBill == null && PendingBills.Any())
                    SelectedBill = PendingBills.First();
            }
            IsBusy = false;
        }

        [RelayCommand]
        public void SelectCash() => IsCashPayment = true;

        [RelayCommand]
        public void SelectTransfer() => IsCashPayment = false;

        [RelayCommand]
        public async Task PayAndPrintAsync()
        {
            if (SelectedBill == null) return;

            // Kiểm tra kỹ lại lần cuối cho chắc
            if (IsCashPayment && ChangeAmount < 0)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Số tiền khách đưa chưa đủ!", "OK");
                return;
            }

            IsBusy = true;

            var payload = new CheckoutRequestDto
            {
                PaymentMethod = IsCashPayment ? "Tiền mặt" : "Chuyển khoản"
            };

            // Gọi API Checkout (Backend của bạn dùng POST)
            var response = await _apiService.PostAsync<HoaDonModel>(ApiConfig.Checkout(SelectedBill.MaHD), payload);

            if (response.Success)
            {
                await Application.Current.MainPage.DisplayAlert("Thành công",
                    $"Thanh toán thành công đơn {SelectedBill.MaHD}\nSố tiền: {SelectedBill.FormattedTotal}", "OK");

                // In hóa đơn (Giả lập)
                // PrintService.Print(SelectedBill);

                // Chuyển hướng sang Báo cáo doanh thu
                await Application.Current.MainPage.Navigation.PushAsync(new RevenueReportPage());

                // Refresh lại danh sách (xóa đơn đã xong)
                await LoadPendingBillsAsync();
                SelectedBill = null;
                CustomerPayAmount = "";
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi thanh toán", response.Message, "OK");
            }

            IsBusy = false;
        }
    }
}