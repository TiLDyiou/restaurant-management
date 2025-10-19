using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurentManagementAPI.Models.Entities
{
    [Table("MONAN")]
    public class MonAn
    {
        [Key]
        [Column("MaMA")]
        [StringLength(5)]
        public string MaMA { get; set; } = string.Empty;

        [Column("TenMA")]
        public string TenMA { get; set; } = string.Empty;

        [Column("DonGia")]
        public decimal DonGia { get; set; }

        [Column("Loai")]
        public string? Loai { get; set; }
    }
}
