using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagementAPI.Models.Entities
{
    [Table("HOADON")]
    public class HoaDon
    {
        [Key]
        [Column("MaHD")]
        [StringLength(5)]
        public string MaHD { get; set; } = string.Empty;

        [Column("MaBan")]
        [StringLength(5)]
        public string MaBan { get; set; } = string.Empty;

        [Column("MaNV")]
        [StringLength(5)]
        public string MaNV { get; set; } = string.Empty;

        [Column("NgayLap")]
        public DateTime? NgayLap { get; set; }

        [Column("TongTien")]
        public decimal TongTien { get; set; }

        [Column("PaymentMethod")]
        public string? PaymentMethod { get; set; }

        [Column("TrangThai")]
        public string? TrangThai { get; set; }

        
        public NhanVien? NhanVien { get; set; }
        public Ban? Ban { get; set; }
        public ICollection<ChiTietHoaDon>? ChiTietHoaDons { get; set; } // 1-n
    }
}
