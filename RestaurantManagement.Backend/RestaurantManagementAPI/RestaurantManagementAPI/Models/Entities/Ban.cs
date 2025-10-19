using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurentManagementAPI.Models.Entities
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
    }
}
