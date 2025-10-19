using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurentManagementAPI.Models.Entities
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
    }
}
