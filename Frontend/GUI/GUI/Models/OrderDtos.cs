
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class CreateHoaDonDto
    {
        [JsonPropertyName("maBan")]
        public string? MaBan { get; set; }

        [JsonPropertyName("maNV")]
        public string MaNV { get; set; }

        [JsonPropertyName("chiTietHoaDons")]
        public List<ChiTietHoaDonDto> ChiTietHoaDons { get; set; }
    }

    public class ChiTietHoaDonDto
    {
        [JsonPropertyName("maMA")]
        public string? MaMA { get; set; }

        [JsonPropertyName("soLuong")]
        public int SoLuong { get; set; }
    }


    public class HoaDonDto
    {
        [JsonPropertyName("maHD")]
        public string MaHD { get; set; }

        [JsonPropertyName("maBan")]
        public string MaBan { get; set; }

        [JsonPropertyName("trangThai")]
        public string TrangThai { get; set; }

        [JsonPropertyName("ngayLap")]
        public DateTime? NgayLap { get; set; }

        [JsonPropertyName("chiTietHoaDons")]
        public List<ChiTietHoaDonViewDto> ChiTietHoaDons { get; set; }

        [JsonPropertyName("tongTien")]
        public decimal? TongTien { get; set; }

        [JsonIgnore]
        public string TableName => $"Bàn {MaBan}";

        [JsonIgnore]
        public string FormattedTotal => TongTien.HasValue ? $"{TongTien.Value:N0} đ" : "0 đ";
    }


    public partial class ChiTietHoaDonViewDto : ObservableObject
    {
        [JsonPropertyName("maMA")]
        public string MaMA { get; set; }

        [JsonPropertyName("tenMA")]
        public string TenMA { get; set; }

        [JsonPropertyName("soLuong")]
        public int SoLuong { get; set; }
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDone))] 
        [JsonPropertyName("trangThai")]
        private string trangThai;
        public bool IsDone => TrangThai == "Đã xong" || TrangThai == "Hết món";
    }
}
