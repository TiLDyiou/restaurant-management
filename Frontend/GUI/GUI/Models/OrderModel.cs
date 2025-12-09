using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    // Class chi tiết món ăn (tương ứng với ChiTietHoaDonViewDto của API)
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

        // Quan trọng: Dùng ObservableProperty để UI tự cập nhật khi đổi trạng thái
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDone))] // Khi TrangThai đổi -> IsDone tự đổi theo
        [JsonPropertyName("trangThai")]
        private string trangThai;

        // --- Thuộc tính hỗ trợ Logic & UI ---

        // Thuộc tính này để ViewModel kiểm tra món đã xong chưa
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
        public DateTime NgayLap { get; set; }

        [JsonPropertyName("tongTien")]
        public decimal TongTien { get; set; }

        [JsonPropertyName("trangThai")]
        public string TrangThai { get; set; }

        [JsonPropertyName("chiTietHoaDons")]
        public List<ChiTietHoaDonModel> ChiTietHoaDons { get; set; } = new();

        // --- Helpers ---
        public string TableName => $"Bàn {MaBan}";
        public string FormattedTotal => $"{TongTien:N0} đ";
    }

    // DTO để gửi đi khi cập nhật trạng thái món
    public class UpdateOrderItemStatusDto
    {
        [JsonPropertyName("newStatus")]
        public string NewStatus { get; set; }
    }
}