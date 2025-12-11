using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementAPI.DTOs.MonAnDtos
{
    public class UpdateOrderStatusDto
    {
        [Required]
        public string NewStatus { get; set; } = string.Empty;
    }
}