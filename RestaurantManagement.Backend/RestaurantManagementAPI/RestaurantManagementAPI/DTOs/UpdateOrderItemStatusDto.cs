using System.ComponentModel.DataAnnotations;

namespace RestaurentManagementAPI.DTOs.MonAnDtos
{
    public class UpdateOrderItemStatusDto
    {
        [Required]
        public string NewStatus { get; set; } = string.Empty;
    }
}