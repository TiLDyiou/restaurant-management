using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Diagnostics;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class BillGenerationViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private const string MY_BANK_ID = "TCB";
        private const string MY_ACCOUNT_NO = "93245230306";
        private const string QR_TEMPLATE = "compact2";

        [ObservableProperty]
        private ObservableCollection<HoaDonModel> pendingBills;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ChangeAmount), nameof(ShowChange), nameof(QrCodeUrl))]
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
                return $"https://img.vietqr.io/image/{MY_BANK_ID}-{MY_ACCOUNT_NO}-{QR_TEMPLATE}.png?amount={SelectedBill.TongTien}&addInfo=Thanh toán HD {SelectedBill.MaHD}";
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
                // Đọc chuỗi JSON thô trước để tránh lỗi convert ngầm
                var json = await _httpClient.GetStringAsync(ApiConfig.GetAllOrders);
                // Cấu hình chấp nhận mọi định dạng chữ hoa/thường
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
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
                Debug.WriteLine($"[BILL] Lỗi tải hóa đơn: {ex.Message}");
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
                Debug.WriteLine($"[BILL] Đang thanh toán {SelectedBill.MaHD}...");

                var response = await _httpClient.PutAsJsonAsync(ApiConfig.Checkout(SelectedBill.MaHD), requestDto);

                if (response.IsSuccessStatusCode)
                {
                    var finalBill = await response.Content.ReadFromJsonAsync<HoaDonModel>();

                    Debug.WriteLine($"[BILL] ✅ Thanh toán thành công {finalBill.MaHD}");

                    // ===== QUAN TRỌNG: BROADCAST EVENT ĐẾN TẤT CẢ LISTENERS =====
                    PaymentEventService.NotifyPaymentCompleted(
                        maHD: finalBill.MaHD,
                        tongTien: finalBill.TongTien,
                        tableName: finalBill.TableName,
                        paymentMethod: method
                    );
                    Debug.WriteLine($"[BILL] 📢 Đã broadcast payment event cho {finalBill.MaHD}");
                    // ============================================================

                    // Thông báo ngắn gọn
                    string message = $"Đã thanh toán thành công cho {finalBill.TableName}.\n" +
                                     "Hệ thống đang in hóa đơn...";
                    await Application.Current.MainPage.DisplayAlert("Thành công", message, "OK");

                    // Xóa khỏi danh sách chờ thanh toán
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
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[BILL] ❌ Lỗi API: {errorContent}");
                    await Application.Current.MainPage.DisplayAlert("Lỗi API", errorContent, "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BILL] ❌ Lỗi hệ thống: {ex.Message}");
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