using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public partial class HoaDonModel
    {
        [JsonPropertyName("maHD")]
        public string? MaHD { get; set; }

        [JsonPropertyName("maBan")]
        public string MaBan { get; set; }

        public string TableName => $"Bàn {MaBan?.Replace("B", "")}"; // Helper hiển thị

        [JsonPropertyName("tongTien")]
        public decimal TongTien { get; set; }

        public string FormattedTotal => $"{TongTien:N0} VNĐ";

        [JsonPropertyName("ngayLap")]
        public DateTime? NgayLap { get; set; }

        [JsonPropertyName("trangThai")]
        public string TrangThai { get; set; }

        [JsonPropertyName("chiTietHoaDons")]
        public List<ChiTietHoaDonModel> ChiTietHoaDons { get; set; }
    }

    public partial class ChiTietHoaDonModel : ObservableObject
    {
        [JsonPropertyName("tenMA")]
        public string TenMA { get; set; }

        [JsonPropertyName("soLuong")]
        public int SoLuong { get; set; }

        [JsonPropertyName("thanhTien")]
        public decimal ThanhTien { get; set; }
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDone))]
        private string _trangThai;

        public bool IsDone => TrangThai == "Đã xong" || TrangThai == "Đã hoàn thành";
    }
}