using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagementAPI.Models.Entities
{
    [Table("DONHANG_ONLINE")]
    public class DonHangOnline
    {
        [Key]
        [Column("MaDH")]
        [StringLength(5)]
        public string MaDH { get; set; } = string.Empty;

        [Column("TenKH")]
        public string? TenKH { get; set; }

        [Column("SDT")]
        public string? SDT { get; set; }

        [Column("NoiDung")]
        public string? NoiDung { get; set; }

        [Column("TrangThai")]
        public string? TrangThai { get; set; }

        [Column("NgayTiepNhan")]
        public DateTime? NgayTiepNhan { get; set; }
    }
}
