using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagementAPI.Models.Entities
{
    [Table("KHO")]
    public class Kho
    {
        [Key]
        [Column("MaNL")]
        [StringLength(5)]
        public string MaNL { get; set; } = string.Empty;

        [Column("TenNL")]
        public string? TenNL { get; set; }

        [Column("DonVi")]
        public string? DonVi { get; set; }

        [Column("SoLuongTon")]
        public int SoLuongTon { get; set; }

        
        public ICollection<ChiTietPhieuNhap>? ChiTietPhieuNhaps { get; set; } // 1-n
    }
}
