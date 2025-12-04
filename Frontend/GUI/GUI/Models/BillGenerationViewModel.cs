using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RestaurantManagementGUI.ViewModels
{
    // Model cho từng món trong bill
    public class BillItem
    {
        public string FoodName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Quantity * Price;
    }

    // Model cho hóa đơn
    public partial class BillModel : ObservableObject
    {
        public string BillId { get; set; }
        public string TableName { get; set; } // Ví dụ: "Bàn 5"
        public DateTime CheckInTime { get; set; }
        public ObservableCollection<BillItem> Items { get; set; }

        public decimal TotalAmount => Items.Sum(i => i.Total);
    }

    public partial class BillGenerationViewModel : ObservableObject
    {
        // Danh sách các bàn đang chờ thanh toán
        [ObservableProperty]
        private ObservableCollection<BillModel> pendingBills;

        // Hóa đơn đang được chọn để thanh toán
        [ObservableProperty]
        private BillModel selectedBill;

        // Quản lý hình thức thanh toán (0: Tiền mặt, 1: Chuyển khoản)
        [ObservableProperty]
        private int paymentMethodIndex = 0;

        // Số tiền khách đưa (để tính tiền thừa)
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ChangeAmount))]
        private decimal customerPayAmount;

        // Tiền thừa trả khách
        public decimal ChangeAmount => SelectedBill != null ? CustomerPayAmount - SelectedBill.TotalAmount : 0;

        public BillGenerationViewModel()
        {
            LoadMockData();
        }

        private void LoadMockData()
        {
            // Tạo dữ liệu giả cho đẹp
            PendingBills = new ObservableCollection<BillModel>
            {
                new BillModel
                {
                    BillId = "HD00125",
                    TableName = "Bàn 5",
                    CheckInTime = DateTime.Now.AddMinutes(-45),
                    Items = new ObservableCollection<BillItem>
                    {
                        new BillItem { FoodName = "Phở Bò Đặc Biệt", Quantity = 2, Price = 55000 },
                        new BillItem { FoodName = "Quẩy", Quantity = 5, Price = 5000 },
                        new BillItem { FoodName = "Trà Đá", Quantity = 2, Price = 3000 }
                    }
                },
                new BillModel
                {
                    BillId = "HD00126",
                    TableName = "Bàn 2",
                    CheckInTime = DateTime.Now.AddMinutes(-20),
                    Items = new ObservableCollection<BillItem>
                    {
                        new BillItem { FoodName = "Bún Bò Huế", Quantity = 1, Price = 45000 },
                        new BillItem { FoodName = "Pepsi", Quantity = 1, Price = 15000 }
                    }
                },
                 new BillModel
                {
                    BillId = "HD00127",
                    TableName = "VIP 1",
                    CheckInTime = DateTime.Now.AddHours(-1),
                    Items = new ObservableCollection<BillItem>
                    {
                        new BillItem { FoodName = "Lẩu Thái", Quantity = 1, Price = 250000 },
                        new BillItem { FoodName = "Bò Mỹ thêm", Quantity = 2, Price = 80000 },
                        new BillItem { FoodName = "Rượu Soju", Quantity = 3, Price = 60000 }
                    }
                }
            };

            // Chọn mặc định cái đầu tiên
            SelectedBill = PendingBills[0];
        }

        [RelayCommand]
        void SelectBill(BillModel bill)
        {
            SelectedBill = bill;
            CustomerPayAmount = 0; // Reset tiền khách đưa
        }

        [RelayCommand]
        async Task PayAndPrint()
        {
            if (SelectedBill == null) return;

            // Logic giả lập thanh toán
            await Shell.Current.DisplayAlert("Thành công", $"Đã thanh toán xong {SelectedBill.TableName}\nTổng: {SelectedBill.TotalAmount:N0} đ", "OK");

            PendingBills.Remove(SelectedBill);
            SelectedBill = PendingBills.FirstOrDefault();
        }
    }
}