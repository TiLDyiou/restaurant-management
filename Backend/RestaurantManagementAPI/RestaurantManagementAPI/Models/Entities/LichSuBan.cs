using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagementAPI.Models.Entities
{
    [Table("LICHSUBAN")]
    public class LichSuBan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("MaBan")]
        [StringLength(5)]
        [Required]
        public string MaBan { get; set; } = string.Empty;

        [Column("TrangThaiCu")]
        [StringLength(50)]
        public string? TrangThaiCu { get; set; }

        [Column("TrangThaiMoi")]
        [StringLength(50)]
        [Required]
        public string TrangThaiMoi { get; set; } = string.Empty;

        [Column("ThoiGian")]
        public DateTime ThoiGian { get; set; } = DateTime.Now;

        [Column("MaNV")]
        [StringLength(5)]
        public string? MaNV { get; set; }

        [ForeignKey("MaBan")]
        public Ban Ban { get; set; } = null!;

        [ForeignKey("MaNV")]
        public NhanVien? NhanVien { get; set; }
    }
}
