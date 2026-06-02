using Microsoft.AspNetCore.Mvc;
using RestaurantManagementAPI.Interfaces;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.DTOs.MonAnDtos;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RestaurantManagementAPI.Controllers
{
    [Route("api/webhooks")]
    [ApiController]
    public class WebhooksController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IRealtimeNotifier _notifier;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(IOrderService orderService, IRealtimeNotifier notifier, ILogger<WebhooksController> logger)
        {
            _orderService = orderService;
            _notifier = notifier;
            _logger = logger;
        }

        // Webhook receiver for SePay (or Casso, PayOS, etc.)
        [HttpPost("sepay")]
        public async Task<IActionResult> SePayWebhook([FromBody] SePayWebhookDto payload)
        {
            if (payload == null)
                return BadRequest(new { success = false, message = "Payload is null" });

            _logger.LogInformation("Received webhook from SePay: {Content}, Amount: {Amount}", payload.content, payload.transferAmount);

            // Ignore money out transactions
            if (payload.transferType != "in")
                return Ok(new { success = true, message = "Not a money-in transaction" });

            // Extract order ID using regex. e.g., "Thanh toan HD HD00012"
            var match = Regex.Match(payload.content ?? "", @"HD\d{5}", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                _logger.LogWarning("Cannot find MaHD in content: {Content}", payload.content);
                return Ok(new { success = true, message = "No Order ID found in content" });
            }

            string maHD = match.Value.ToUpper();

            // Fetch the order to verify amount
            var orderResult = await _orderService.GetOrderByIdAsync(maHD);
            if (!orderResult.Success || orderResult.Data == null)
            {
                _logger.LogWarning("Order {MaHD} not found from webhook.", maHD);
                return Ok(new { success = true, message = "Order not found" });
            }

            var hd = orderResult.Data;
            
            // Check if already paid
            if (hd.TrangThai == "Đã thanh toán")
                return Ok(new { success = true, message = "Order already paid" });

            // Check if amount is enough
            if (payload.transferAmount < hd.TongTien)
            {
                _logger.LogWarning("Insufficient payment for {MaHD}. Expected {Expected}, got {Got}", maHD, hd.TongTien, payload.transferAmount);
                return Ok(new { success = true, message = "Insufficient payment" });
            }

            // Checkout the order
            var checkoutDto = new CheckoutRequestDto { PaymentMethod = "Chuyển khoản (Tự động)" };
            var checkoutResult = await _orderService.CheckoutAsync(maHD, checkoutDto);

            if (checkoutResult.Success)
            {
                _logger.LogInformation("Auto checkout successful for {MaHD}", maHD);
                // Notify the frontend via SignalR/TCP
                await _notifier.NotifyPaymentSuccessAsync(maHD, payload.transferAmount);
            }

            return Ok(new { success = true, message = "Webhook processed successfully" });
        }

        [HttpPost("casso")]
        public async Task<IActionResult> CassoWebhook([FromBody] CassoWebhookPayload payload)
        {
            if (payload == null || payload.error != 0 || payload.data == null)
                return BadRequest(new { success = false, message = "Invalid Casso Payload" });

            foreach (var transaction in payload.data)
            {
                // Only care about money-in transactions (amount > 0)
                if (transaction.amount <= 0) continue;

                var match = Regex.Match(transaction.description ?? "", @"HD\d{5}", RegexOptions.IgnoreCase);
                if (!match.Success) continue;

                string maHD = match.Value.ToUpper();
                var orderResult = await _orderService.GetOrderByIdAsync(maHD);
                
                if (!orderResult.Success || orderResult.Data == null) continue;

                var hd = orderResult.Data;
                if (hd.TrangThai == "Đã thanh toán") continue;
                if (transaction.amount < hd.TongTien) continue;

                var checkoutDto = new CheckoutRequestDto { PaymentMethod = "Chuyển khoản (Casso)" };
                var checkoutResult = await _orderService.CheckoutAsync(maHD, checkoutDto);

                if (checkoutResult.Success)
                {
                    _logger.LogInformation("Auto checkout successful via Casso for {MaHD}", maHD);
                    await _notifier.NotifyPaymentSuccessAsync(maHD, transaction.amount);
                }
            }

            return Ok(new { success = true, message = "Casso Webhook processed" });
        }
    }

    public class CassoWebhookPayload
    {
        public int error { get; set; }
        public List<CassoTransaction> data { get; set; } = new List<CassoTransaction>();
    }

    public class CassoTransaction
    {
        public int id { get; set; }
        public string tid { get; set; }
        public string description { get; set; }
        public decimal amount { get; set; }
        public decimal cusum_balance { get; set; }
        public string when { get; set; }
    }

    public class SePayWebhookDto
    {
        public long id { get; set; }
        public string gateway { get; set; }
        public string transactionDate { get; set; }
        public string accountNumber { get; set; }
        public string code { get; set; }
        public string content { get; set; }
        public string transferType { get; set; }
        public decimal transferAmount { get; set; }
        public decimal accumulated { get; set; }
    }
}
