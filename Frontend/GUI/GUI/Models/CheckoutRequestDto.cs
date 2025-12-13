using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class CheckoutRequestDto
    {
        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod { get; set; }
    }
}