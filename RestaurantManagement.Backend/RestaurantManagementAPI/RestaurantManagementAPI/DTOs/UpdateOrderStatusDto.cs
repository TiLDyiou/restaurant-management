using System.ComponentModel.DataAnnotations;

namespace RestaurentManagementAPI.DTOs.MonAnDtos
{
    public class UpdateOrderStatusDto
    {
        [Required]
        public string NewStatus { get; set; } = string.Empty;
    }
}