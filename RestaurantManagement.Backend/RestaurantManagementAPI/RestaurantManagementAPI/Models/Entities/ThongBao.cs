using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagementAPI.Models.Entities
{
    [Table("THONGBAO")]
    public class ThongBao
    {
        [Key]
        public int Id { get; set; }
        public string NoiDung { get; set; }
        public DateTime ThoiGian { get; set; }
        public bool IsRead { get; set; } = false;
        public string Loai { get; set; }
    }
}