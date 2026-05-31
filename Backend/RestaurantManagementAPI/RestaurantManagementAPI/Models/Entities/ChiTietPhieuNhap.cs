using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagementAPI.Models.Entities
{
    [Table("CHITIETPHIEUNHAP")]
    public class ChiTietPhieuNhap
    {
        [Column("MaPN")]
        public string MaPN { get; set; } = string.Empty;

        [Column("MaNL")]
        public string MaNL { get; set; } = string.Empty;

        [Column("SoLuong")]
        public int SoLuong { get; set; }

        
        public PhieuNhapKho? PhieuNhapKho { get; set; }
        public Kho? Kho { get; set; }
    }
}
