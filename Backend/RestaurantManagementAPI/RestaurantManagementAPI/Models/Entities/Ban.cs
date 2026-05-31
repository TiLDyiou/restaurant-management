using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagementAPI.Models.Entities
{
    [Table("BAN")]
    public class Ban
    {
        [Key]
        [Column("MaBan")]
        [StringLength(5)]
        public string MaBan { get; set; } = string.Empty;

        [Column("TenBan")]
        public string? TenBan { get; set; }

        [Column("TrangThai")]
        public string? TrangThai { get; set; }

        [Column("SucChua")]
        public int SucChua { get; set; } = 4;

        [Column("KhuVuc")]
        [StringLength(50)]
        public string KhuVuc { get; set; } = string.Empty;

        [Column("IsDeleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("MaBanGop")]
        [StringLength(5)]
        public string? MaBanGop { get; set; }

        public ICollection<HoaDon>? HoaDons { get; set; } // 1-n
    }
}
