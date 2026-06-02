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

        [ObservableProperty]
        private ObservableCollection<HoaDonDto> pendingBills = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ChangeAmount), nameof(ShowChange))]
        private HoaDonDto selectedBill;

        [ObservableProperty]
        private string qrCodeUrl;

        [ObservableProperty]
        private string bankName;

        [ObservableProperty]
        private string accountName;

        [ObservableProperty]
        private string accountNumber;

        [ObservableProperty]
        private string paymentDescription;

        [ObservableProperty]
        private bool isQrLoading;

        partial void OnSelectedBillChanged(HoaDonDto value)
        {
            ResetPaymentForm();
            _ = LoadPayOSQrCode(value);
        }

        private async Task LoadPayOSQrCode(HoaDonDto bill)
        {
            if (bill == null)
            {
                QrCodeUrl = "";
                IsQrLoading = false;
                return;
            }

            IsQrLoading = true;
            QrCodeUrl = "";

            try
            {
                var response = await _httpClient.PostAsync($"{ApiConfig.BaseUrl}PayOS/create-payment-link/{bill.MaHD}", null);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PayOSResponse>(_jsonOptions);
                    if (result != null && result.Success)
                    {
                        var accName = result.AccountName ?? "";
                        var desc = result.Description ?? "";
                        
                        AccountName = accName;
                        AccountNumber = result.AccountNumber ?? "";
                        BankName = result.Bin ?? ""; 
                        PaymentDescription = desc;

                        var urlSafeAccName = Uri.EscapeDataString(accName);
                        var urlSafeDesc = Uri.EscapeDataString(desc);
                        QrCodeUrl = $"https://img.vietqr.io/image/{result.Bin}-{result.AccountNumber}-compact2.png?amount={result.Amount}&addInfo={urlSafeDesc}&accountName={urlSafeAccName}";
                        return;
                    }
                }

                QrCodeUrl = "";
            }
            catch (Exception)
            {
                QrCodeUrl = "";
            }
            finally
            {
                IsQrLoading = false;
            }
        }

        public class PayOSResponse
        {
            public bool Success { get; set; }
            public string CheckoutUrl { get; set; }
            public string QrCode { get; set; }
            public string Bin { get; set; }
            public string AccountNumber { get; set; }
            public string AccountName { get; set; }
            public long Amount { get; set; }
            public string Description { get; set; }
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

        public BillGenerationViewModel(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _ = LoadPendingBills();

            TCPSocketClient.Instance.OnPaymentSuccess -= HandlePaymentSuccess;
            TCPSocketClient.Instance.OnPaymentSuccess += HandlePaymentSuccess;
            
            TCPSocketClient.Instance.OnNewOrderReceived -= HandleNewOrder;
            TCPSocketClient.Instance.OnNewOrderReceived += HandleNewOrder;
        }

        private void HandleNewOrder(string maHD)
        {
            _ = LoadPendingBills();
        }

        private void HandlePaymentSuccess(string maHD, decimal amount)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var bill = PendingBills.FirstOrDefault(b => b.MaHD == maHD);
                if (bill != null)
                {
                    PendingBills.Remove(bill);
                    if (SelectedBill?.MaHD == maHD)
                    {
                        SelectedBill = PendingBills.FirstOrDefault();
                        ResetPaymentForm();
                    }
                    
                    PaymentEventService.NotifyPaymentCompleted(maHD, amount, bill.TableName, "Chuyển khoản (Tự động)");
                    await Application.Current.MainPage.DisplayAlert("💰 Ting ting!", $"Khách hàng vừa thanh toán thành công {amount:N0}đ cho đơn {maHD} qua mã QR.", "Tuyệt vời");
                }
            });
        }

        public string? TargetTableId { get; set; }

        public async Task LoadPendingBills()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<PaginatedResult<HoaDonDto>>>(ApiConfig.Orders, _jsonOptions);
                if (response != null && response.Success && response.Data != null && response.Data.Items != null)
                {
                    var pending = response.Data.Items
                        .Where(b => b.TrangThai != "Đã thanh toán" && b.TrangThai != "Đã hủy")
                        .OrderByDescending(b => b.NgayLap).ToList();

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PendingBills.Clear();
                        foreach (var bill in pending) PendingBills.Add(bill);
                        
                        if (!string.IsNullOrEmpty(TargetTableId))
                        {
                            SelectedBill = PendingBills.FirstOrDefault(b => b.MaBan == TargetTableId) ?? PendingBills.FirstOrDefault();
                            TargetTableId = null;
                        }
                        else if (SelectedBill == null && PendingBills.Any()) 
                        {
                            SelectedBill = PendingBills[0];
                        }
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

            if (IsCashPayment)
            {
                if (!decimal.TryParse(CustomerPayAmount, out decimal payAmount) || payAmount < (SelectedBill.TongTien ?? 0))
                {
                    await Application.Current.MainPage.DisplayAlert("Lỗi", "Số tiền khách đưa không hợp lệ hoặc không đủ để thanh toán.", "OK");
                    return;
                }
            }

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