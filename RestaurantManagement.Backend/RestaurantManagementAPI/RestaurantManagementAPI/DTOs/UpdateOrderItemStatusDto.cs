using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementAPI.DTOs.MonAnDtos
{
    public class UpdateOrderItemStatusDto
    {
        [Required]
        public string NewStatus { get; set; } = string.Empty;
    }
}