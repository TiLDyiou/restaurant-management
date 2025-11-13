using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurentManagementAPI.Models.Entities
{
    [Table("EmailConfirmations")]
    public class EmailConfirmation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(5)]
        public string MaNV { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public DateTime Expiry { get; set; }
    }
}
