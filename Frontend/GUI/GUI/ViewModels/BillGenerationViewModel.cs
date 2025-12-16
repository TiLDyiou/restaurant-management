using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services; 
using RestaurantManagementGUI.Views; 
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class BillGenerationViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        private const string MY_BANK_ID = "VCB";
        private const string MY_ACCOUNT_NO = "9969390384";
        private const string QR_TEMPLATE = "compact2";

        [ObservableProperty]
        private ObservableCollection<HoaDonDto> pendingBills = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(QrCodeUrl), nameof(ChangeAmount), nameof(ShowChange))]
        private HoaDonDto selectedBill;

        partial void OnSelectedBillChanged(HoaDonDto value) => ResetPaymentForm();

        public string QrCodeUrl
        {
            get
            {
                if (SelectedBill == null) return "";
                long amount = (long)(SelectedBill.TongTien ?? 0);
                string addInfo = $"Thanh toan HD {SelectedBill.MaHD}";
                return $"https://img.vietqr.io/image/{MY_BANK_ID}-{MY_ACCOUNT_NO}-{QR_TEMPLATE}.png?amount={amount}&addInfo={Uri.EscapeDataString(addInfo)}";
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
                    return payAmount - (SelectedBill.TongTien ?? 0);
                }
                return 0;
            }
        }
        public bool ShowChange => IsCashPayment && ChangeAmount > 0;

        public BillGenerationViewModel()
        {
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
            _httpClient = new HttpClient(handler) { BaseAddress = new Uri(ApiConfig.BaseUrl) };
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _ = LoadPendingBills();
        }

        public async Task LoadPendingBills()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<HoaDonDto>>>(ApiConfig.Orders, _jsonOptions);
                if (response != null && response.Success && response.Data != null)
                {
                    var pending = response.Data
                        .Where(b => b.TrangThai != "Đã thanh toán" && b.TrangThai != "Đã hủy")
                        .OrderByDescending(b => b.NgayLap).ToList();

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PendingBills.Clear();
                        foreach (var bill in pending) PendingBills.Add(bill);
                        if (SelectedBill == null && PendingBills.Any()) SelectedBill = PendingBills[0];
                    });
                }
            }
            catch { }
        }

        [RelayCommand]
        void SelectCashPayment() => IsCashPayment = true;

        [RelayCommand]
        void SelectTransferPayment() { IsCashPayment = false; CustomerPayAmount = ""; }

        [RelayCommand]
        async Task PayAndPrint()
        {
            if (SelectedBill == null) return;

            string method = IsCashPayment ? "Tiền mặt" : "Chuyển khoản";
            var requestDto = new { PaymentMethod = method };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(ApiConfig.Checkout(SelectedBill.MaHD), requestDto);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<HoaDonDto>>(_jsonOptions);
                    var finalBill = result.Data;
                    PaymentEventService.NotifyPaymentCompleted(
                        finalBill.MaHD,
                        finalBill.TongTien ?? 0,
                        SelectedBill.TableName,
                        method
                    );

                    await Application.Current.MainPage.DisplayAlert("Thành công", $"Đã thanh toán đơn {finalBill.MaHD}", "OK");

                    PendingBills.Remove(SelectedBill);
                    SelectedBill = PendingBills.FirstOrDefault();
                    ResetPaymentForm();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Lỗi", "Thanh toán thất bại", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", ex.Message, "OK");
            }
        }

        private void ResetPaymentForm() { CustomerPayAmount = ""; IsCashPayment = true; }
    }
}