using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurentManagementAPI.Models.Entities
{
    [Table("PHIEUNHAPKHO")]
    public class PhieuNhapKho
    {
        [Key]
        [Column("MaPN")]
        [StringLength(5)]
        public string MaPN { get; set; } = string.Empty;

        [Column("MaNV")]
        [StringLength(5)]
        public string? MaNV { get; set; }

        [Column("NgayNhap")]
        public DateTime? NgayNhap { get; set; }

        [Column("GhiChu")]
        public string? GhiChu { get; set; }

        
        public NhanVien? NhanVien { get; set; }
        public ICollection<ChiTietPhieuNhap>? ChiTietPhieuNhaps { get; set; } // 1-n
    }
}
