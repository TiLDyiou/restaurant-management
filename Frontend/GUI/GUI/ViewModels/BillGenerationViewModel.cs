using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Helpers; // Dùng ApiConfig
using RestaurantManagementGUI.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class BillGenerationViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;

        // --- PROPERTIES ---

        [ObservableProperty]
        private ObservableCollection<HoaDonModel> pendingBills;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ChangeAmount))] // Tính lại tiền thừa khi đổi hóa đơn
        private HoaDonModel selectedBill;

        // Binding với RadioButton (True = Tiền mặt, False = Chuyển khoản)
        [ObservableProperty]
        private bool isCashPayment = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ChangeAmount))] // Tính lại tiền thừa khi nhập tiền
        private decimal? customerPayAmount;

        // Logic tính tiền thừa (Chỉ tính khi chọn Tiền mặt)
        public decimal ChangeAmount
        {
            get
            {
                if (SelectedBill == null || CustomerPayAmount == null) return 0;
                var change = CustomerPayAmount.Value - SelectedBill.TongTien;
                return change > 0 ? change : 0;
            }
        }

        // --- CONSTRUCTOR ---
        public BillGenerationViewModel()
        {
            // Cấu hình HttpClient (Bỏ qua SSL localhost)
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);

            PendingBills = new ObservableCollection<HoaDonModel>();

            // Tải dữ liệu khi mở màn hình
            LoadPendingBills();
        }

        // --- METHODS ---

        // Hàm tải danh sách hóa đơn từ API
        public async void LoadPendingBills()
        {
            try
            {
                // Gọi API lấy tất cả đơn
                var response = await _httpClient.GetAsync(ApiConfig.GetAllOrders);
                if (response.IsSuccessStatusCode)
                {
                    var allBills = await response.Content.ReadFromJsonAsync<List<HoaDonModel>>();

                    // Lọc: Chỉ lấy những đơn CHƯA thanh toán
                    var pending = allBills.Where(b => b.TrangThai != "Đã thanh toán").OrderByDescending(b => b.NgayLap).ToList();

                    PendingBills.Clear();
                    foreach (var bill in pending)
                    {
                        PendingBills.Add(bill);
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi kết nối", $"Không tải được danh sách đơn: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        void SelectBill(HoaDonModel bill)
        {
            SelectedBill = bill;
            CustomerPayAmount = null; // Reset tiền khách đưa khi chọn đơn mới
        }

        [RelayCommand]
        async Task PayAndPrint()
        {
            if (SelectedBill == null)
            {
                await Application.Current.MainPage.DisplayAlert("Thông báo", "Vui lòng chọn hóa đơn cần thanh toán", "OK");
                return;
            }

            // 1. Xác định phương thức thanh toán
            string method = IsCashPayment ? "Tiền mặt" : "Chuyển khoản (QR)";

            // 2. Tạo body request
            var requestDto = new CheckoutRequestDto { PaymentMethod = method };

            try
            {
                // 3. Gọi API Checkout
                var response = await _httpClient.PutAsJsonAsync(ApiConfig.Checkout(SelectedBill.MaHD), requestDto);

                if (response.IsSuccessStatusCode)
                {
                    // Lấy hóa đơn mới nhất từ server trả về (để in)
                    var finalBill = await response.Content.ReadFromJsonAsync<HoaDonModel>();

                    // 4. Thông báo & Giả lập In
                    await Application.Current.MainPage.DisplayAlert("Thành công",
                        $"Đã thanh toán hóa đơn {finalBill.MaHD}.\nTổng tiền: {finalBill.FormattedTotal}\nHệ thống đang in...", "OK");

                    // 5. Cập nhật UI: Xóa đơn khỏi danh sách chờ
                    PendingBills.Remove(SelectedBill);
                    SelectedBill = null;
                    CustomerPayAmount = null;
                }
                else
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    await Application.Current.MainPage.DisplayAlert("Lỗi thanh toán", errorMsg, "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi hệ thống", ex.Message, "OK");
            }
        }
    }
}