using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurentManagementAPI.Models.Entities
{
    [Table("CHITIETHOADON")]
    public class ChiTietHoaDon
    {
        [Column("MaHD")]
        [StringLength(5)]
        public string MaHD { get; set; } = string.Empty;

        [Column("MaMA")]
        [StringLength(5)]
        public string MaMA { get; set; } = string.Empty;

        [Column("SoLuong")]
        public int SoLuong { get; set; }

        [Column("DonGia")]
        public decimal DonGia { get; set; }

        [Column("ThanhTien")]
        public decimal ThanhTien { get; set; } // Computed column
    }
}
