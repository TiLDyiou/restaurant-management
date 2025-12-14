
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RestaurantManagementGUI.Models
{
    // DTO để gửi đi khi cập nhật trạng thái món
    public class UpdateOrderItemStatusDto
    {
        [JsonPropertyName("newStatus")]
        public string NewStatus { get; set; }
    }
    public class UpdateOrderStatusDto { public string NewStatus { get; set; } }
}
