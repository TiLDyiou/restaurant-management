using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.Views; // Để mở RevenueReportPage
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Diagnostics;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class BillGenerationViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;

        // Thông tin ngân hàng (Thay bằng của bạn)
        private const string MY_BANK_ID = "TCB";
        private const string MY_ACCOUNT_NO = "93245230306";
        private const string QR_TEMPLATE = "compact2";

        [ObservableProperty]
        private ObservableCollection<HoaDonModel> pendingBills;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ChangeAmount), nameof(ShowChange), nameof(QrCodeUrl))]
        private HoaDonModel selectedBill;

        partial void OnSelectedBillChanged(HoaDonModel value)
        {
            ResetPaymentForm();
            OnPropertyChanged(nameof(QrCodeUrl));
        }

        public string QrCodeUrl
        {
            get
            {
                if (SelectedBill == null) return "";
                return $"https://img.vietqr.io/image/{MY_BANK_ID}-{MY_ACCOUNT_NO}-{QR_TEMPLATE}.png?amount={SelectedBill.TongTien}&addInfo=Thanh toan HD {SelectedBill.MaHD}";
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
                if (SelectedBill == null || string.IsNullOrWhiteSpace(CustomerPayAmount)) return 0;
                if (decimal.TryParse(CustomerPayAmount, out decimal payAmount))
                {
                    var change = payAmount - SelectedBill.TongTien;
                    return change > 0 ? change : 0;
                }
                return 0;
            }
        }

        public bool ShowChange => SelectedBill != null && !string.IsNullOrWhiteSpace(CustomerPayAmount) && decimal.TryParse(CustomerPayAmount, out _);

        public BillGenerationViewModel()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
            PendingBills = new ObservableCollection<HoaDonModel>();
            LoadPendingBills();
        }

        public async void LoadPendingBills()
        {
            try
            {
                var json = await _httpClient.GetStringAsync(ApiConfig.GetAllOrders);
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allBills = System.Text.Json.JsonSerializer.Deserialize<List<HoaDonModel>>(json, options);

                if (allBills != null)
                {
                    var pending = allBills
                        .Where(b => b.TrangThai != "Đã thanh toán")
                        .OrderByDescending(b => b.NgayLap)
                        .ToList();

                    PendingBills.Clear();
                    foreach (var bill in pending) PendingBills.Add(bill);

                    if (SelectedBill == null && PendingBills.Any())
                        SelectedBill = PendingBills[0];
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BILL ERROR] {ex.Message}");
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
                // 1. Gọi API Thanh Toán
                var response = await _httpClient.PutAsJsonAsync(ApiConfig.Checkout(SelectedBill.MaHD), requestDto);

                if (response.IsSuccessStatusCode)
                {
                    var finalBill = await response.Content.ReadFromJsonAsync<HoaDonModel>();

                    await Application.Current.MainPage.DisplayAlert("Thành công",
                        $"Đã thanh toán xong đơn {finalBill.MaHD}!\nSố tiền: {finalBill.TongTien:N0} ₫", "OK");

                    // 2. Xóa đơn khỏi danh sách
                    var index = PendingBills.IndexOf(SelectedBill);
                    PendingBills.Remove(SelectedBill);
                    if (PendingBills.Any())
                        SelectedBill = index < PendingBills.Count ? PendingBills[index] : PendingBills.FirstOrDefault();
                    else
                    {
                        SelectedBill = null;
                        ResetPaymentForm();
                    }

                    // ============================================================
                    // 3. CHUYỂN HƯỚNG SANG TRANG BÁO CÁO DOANH THU
                    // ============================================================
                    await Application.Current.MainPage.Navigation.PushAsync(new RevenueReportPage());
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    await Application.Current.MainPage.DisplayAlert("Lỗi API", err, "OK");
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
}