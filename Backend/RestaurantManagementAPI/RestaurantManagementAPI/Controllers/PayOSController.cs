using Microsoft.AspNetCore.Mvc;
using PayOS;
using PayOS.Models.Webhooks;
using PayOS.Models.V2.PaymentRequests;
using RestaurantManagementAPI.Interfaces;
using RestaurantManagementAPI.DTOs.MonAnDtos;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RestaurantManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PayOSController : ControllerBase
    {
        private readonly PayOSClient _payOS;
        private readonly IOrderService _orderService;

        public PayOSController(PayOSClient payOS, IOrderService orderService)
        {
            _payOS = payOS;
            _orderService = orderService;
        }

        [HttpPost("create-payment-link/{maHD}")]
        public async Task<IActionResult> CreatePaymentLink(string maHD)
        {
            try
            {
                var orderResult = await _orderService.GetOrderByIdAsync(maHD);
                if (!orderResult.Success || orderResult.Data == null)
                    return NotFound(new { success = false, message = "Không tìm thấy hóa đơn" });

                var hd = orderResult.Data;
                if (hd.TrangThai == "Đã thanh toán")
                    return BadRequest(new { success = false, message = "Hóa đơn đã được thanh toán" });

                int amount = (int)hd.TongTien;
                
                // PayOS orderCode requires a numeric ID. We can strip "HD" and convert to integer.
                // Assuming MaHD is format "HD00012"
                long orderCode = long.Parse(maHD.Replace("HD", ""));

                var items = new List<PaymentLinkItem>
                {
                    new PaymentLinkItem { Name = "Hóa đơn " + maHD, Quantity = 1, Price = amount }
                };

                // Note: The cancel and return URL can be dummy URLs for a desktop/mobile app
                var request = new CreatePaymentLinkRequest
                {
                    OrderCode = orderCode,
                    Amount = amount,
                    Description = "Thanh toan " + maHD,
                    Items = items,
                    CancelUrl = "http://localhost/cancel",
                    ReturnUrl = "http://localhost/success"
                };

                var createPayment = await _payOS.PaymentRequests.CreateAsync(request);

                return Ok(new
                {
                    success = true,
                    checkoutUrl = createPayment.CheckoutUrl,
                    qrCode = createPayment.QrCode,
                    bin = createPayment.Bin,
                    accountNumber = createPayment.AccountNumber,
                    accountName = createPayment.AccountName,
                    amount = createPayment.Amount,
                    description = createPayment.Description
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> PayOSWebhook([FromBody] Webhook webhookBody, [FromServices] IRealtimeNotifier notifier)
        {
            try
            {
                var data = await _payOS.Webhooks.VerifyAsync(webhookBody);

                if (data.Code == "00" || webhookBody.Code == "00")
                {
                    // Payment successful
                    string maHD = "HD" + data.OrderCode.ToString("D5");
                    
                    var orderResult = await _orderService.GetOrderByIdAsync(maHD);
                    if (orderResult.Success && orderResult.Data != null)
                    {
                        var hd = orderResult.Data;
                        if (hd.TrangThai != "Đã thanh toán" && data.Amount >= (int)hd.TongTien)
                        {
                            var checkoutDto = new CheckoutRequestDto { PaymentMethod = "Chuyển khoản (PayOS)" };
                            var checkoutResult = await _orderService.CheckoutAsync(maHD, checkoutDto);

                            if (checkoutResult.Success)
                            {
                                await notifier.NotifyPaymentSuccessAsync(maHD, data.Amount);
                            }
                        }
                    }
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
