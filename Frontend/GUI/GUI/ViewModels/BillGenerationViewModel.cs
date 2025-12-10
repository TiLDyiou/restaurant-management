using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class BillGenerationViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;

        // ==========================================
        // 👇 CẤU HÌNH THÔNG TIN NGÂN HÀNG CỦA BẠN 👇
        // ==========================================
        private const string MY_BANK_ID = "TCB";       // Mã ngân hàng (VD: MB, VCB, TCB, ACB...)
        private const string MY_ACCOUNT_NO = "93245230306"; // Số tài khoản của bạn
        private const string QR_TEMPLATE = "compact2"; // Kiểu giao diện QR (compact2 là gọn đẹp nhất)
        // ==========================================

        // --- PROPERTIES ---

        [ObservableProperty]
        private ObservableCollection<HoaDonModel> pendingBills;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ChangeAmount), nameof(ShowChange), nameof(QrCodeUrl))] // Thêm update QrCodeUrl
        private HoaDonModel selectedBill;

        // Logic tự động cập nhật QR và Reset form khi đổi bàn
        partial void OnSelectedBillChanged(HoaDonModel value)
        {
            ResetPaymentForm();
            // Kích hoạt cập nhật lại mã QR
            OnPropertyChanged(nameof(QrCodeUrl));
        }

        // Property sinh Link ảnh QR Code động
        public string QrCodeUrl
        {
            get
            {
                if (SelectedBill == null) return "";

                // Tạo nội dung chuyển khoản: "TT HD {Mã Hóa Đơn}"
                // Lưu ý: TongTien phải là số (decimal/int)
                return $"https://img.vietqr.io/image/{MY_BANK_ID}-{MY_ACCOUNT_NO}-{QR_TEMPLATE}.png?amount={SelectedBill.TongTien}&addInfo=TT HD {SelectedBill.MaHD}";
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsTransferPayment))]
        private bool isCashPayment = true;

        public bool IsTransferPayment => !IsCashPayment;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ChangeAmount), nameof(ShowChange))]
        private string customerPayAmount;

        public decimal ChangeAmount
        {
            get
            {
                if (SelectedBill == null || string.IsNullOrWhiteSpace(CustomerPayAmount))
                    return 0;

                if (decimal.TryParse(CustomerPayAmount, out decimal payAmount))
                {
                    var change = payAmount - SelectedBill.TongTien;
                    return change > 0 ? change : 0;
                }
                return 0;
            }
        }

        public bool ShowChange =>
            SelectedBill != null &&
            !string.IsNullOrWhiteSpace(CustomerPayAmount) &&
            decimal.TryParse(CustomerPayAmount, out _);

        // --- CONSTRUCTOR ---
        public BillGenerationViewModel()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);

            PendingBills = new ObservableCollection<HoaDonModel>();
            LoadPendingBills();
        }

        // --- METHODS ---

        public async void LoadPendingBills()
        {
            try
            {
                var response = await _httpClient.GetAsync(ApiConfig.GetAllOrders);
                if (response.IsSuccessStatusCode)
                {
                    var allBills = await response.Content.ReadFromJsonAsync<List<HoaDonModel>>();

                    var pending = allBills
                        .Where(b => b.TrangThai != "Đã thanh toán")
                        .OrderByDescending(b => b.NgayLap)
                        .ToList();

                    PendingBills.Clear();
                    foreach (var bill in pending)
                    {
                        PendingBills.Add(bill);
                    }

                    if (SelectedBill == null && PendingBills.Any())
                    {
                        SelectedBill = PendingBills[0];
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", $"Không tải được danh sách: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        void SelectCashPayment() => IsCashPayment = true;

        [RelayCommand]
        void SelectTransferPayment()
        {
            IsCashPayment = false;
            CustomerPayAmount = "";
        }

        [RelayCommand]
        async Task PayAndPrint()
        {
            if (SelectedBill == null)
            {
                await Application.Current.MainPage.DisplayAlert("Thông báo", "Vui lòng chọn bàn cần thanh toán", "OK");
                return;
            }

            if (IsCashPayment)
            {
                if (string.IsNullOrWhiteSpace(CustomerPayAmount) ||
                    !decimal.TryParse(CustomerPayAmount, out decimal payAmount) ||
                    payAmount < SelectedBill.TongTien)
                {
                    await Application.Current.MainPage.DisplayAlert("Lỗi", "Tiền khách đưa không đủ", "OK");
                    return;
                }
            }

            string method = IsCashPayment ? "Tiền mặt" : "Chuyển khoản (QR)";
            var requestDto = new CheckoutRequestDto { PaymentMethod = method };

            try
            {
                var response = await _httpClient.PutAsJsonAsync(ApiConfig.Checkout(SelectedBill.MaHD), requestDto);

                if (response.IsSuccessStatusCode)
                {
                    var finalBill = await response.Content.ReadFromJsonAsync<HoaDonModel>();

                    // Thông báo ngắn gọn
                    string message = $"Đã thanh toán thành công cho bàn {finalBill.TableName}.\n" +
                                     "Hệ thống đang in hóa đơn...";

                    await Application.Current.MainPage.DisplayAlert("Thành công", message, "OK");

                    var index = PendingBills.IndexOf(SelectedBill);
                    PendingBills.Remove(SelectedBill);

                    if (PendingBills.Any())
                    {
                        SelectedBill = index < PendingBills.Count ? PendingBills[index] : PendingBills.FirstOrDefault();
                    }
                    else
                    {
                        SelectedBill = null;
                        ResetPaymentForm();
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Lỗi API", await response.Content.ReadAsStringAsync(), "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi hệ thống", ex.Message, "OK");
            }
        }

        private void ResetPaymentForm()
        {
            CustomerPayAmount = "";
            IsCashPayment = true;
            OnPropertyChanged(nameof(ChangeAmount));
            OnPropertyChanged(nameof(ShowChange));
        }
    }

    public class CheckoutRequestDto
    {
        public string PaymentMethod { get; set; }
    }
}