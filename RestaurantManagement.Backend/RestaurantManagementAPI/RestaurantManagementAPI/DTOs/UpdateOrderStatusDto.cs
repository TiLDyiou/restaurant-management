using System.ComponentModel.DataAnnotations;

namespace RestaurentManagementAPI.DTOs.MonAnDtos
{
    public class UpdateOrderStatusDto
    {
        [Required]
        public string NewStatus { get; set; } = string.Empty;
        // Ví dụ: "Đã hoàn thành", "Sẵn sàng phục vụ"
    }
}