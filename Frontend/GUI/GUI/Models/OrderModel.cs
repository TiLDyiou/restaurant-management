using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public partial class ChiTietHoaDonModel : ObservableObject
    {
        [JsonPropertyName("maMA")]
        public string MaMA { get; set; }

        [JsonPropertyName("tenMA")]
        public string TenMA { get; set; }

        [JsonPropertyName("soLuong")]
        public int SoLuong { get; set; }

        [JsonPropertyName("donGia")]
        public decimal DonGia { get; set; }

        [JsonPropertyName("thanhTien")]
        public decimal ThanhTien { get; set; }

        // Dùng ObservableProperty để UI tự cập nhật khi đổi trạng thái
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDone))]
        [JsonPropertyName("trangThai")]
        private string trangThai;
        public bool IsDone => TrangThai == "Đã xong";
        public string FormattedTotal => $"{ThanhTien:N0} đ";
    }

    // Class hóa đơn chính
    public partial class HoaDonModel : ObservableObject
    {
        [JsonPropertyName("maHD")]
        public string MaHD { get; set; }

        [JsonPropertyName("maBan")]
        public object MaBan { get; set; }

        [JsonPropertyName("maNV")]
        public string MaNV { get; set; }

        [JsonPropertyName("ngayLap")]
        public DateTime? NgayLap { get; set; }

        [JsonPropertyName("tongTien")]
        public decimal TongTien { get; set; }

        [JsonPropertyName("trangThai")]
        public string TrangThai { get; set; }

        [JsonPropertyName("chiTietHoaDons")]
        public List<ChiTietHoaDonModel> ChiTietHoaDons { get; set; } = new();

        [ObservableProperty]
        private bool isSelected;
        public string TableName => $"Bàn {MaBan}";
        public string FormattedTotal => $"{TongTien:N0} đ";
    }
}