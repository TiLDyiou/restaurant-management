
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RestaurantManagementGUI.Models
{
    public class UpdateOrderItemStatusDto
    {
        [JsonPropertyName("newStatus")]
        public string NewStatus { get; set; }
    }
    public class UpdateOrderStatusDto { public string NewStatus { get; set; } }
}
