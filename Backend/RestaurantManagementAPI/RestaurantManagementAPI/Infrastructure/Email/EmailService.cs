using Microsoft.Extensions.Configuration;
using RestaurantManagementAPI.Interfaces;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RestaurantManagementAPI.Infrastructure.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly string _apiKey; 
        private readonly HttpClient _httpClient;

        public EmailService(IConfiguration config)
        {
            _config = config; 
            var emailSettings = _config.GetSection("EmailSettings");
            
            _senderEmail = emailSettings["SenderEmail"]!;
            _senderName = emailSettings["SenderName"] ?? "Nhà hàng";
            
            // Tận dụng biến AppPassword trong appsettings.json để lưu API Key của Brevo 
            // (Bạn đỡ phải sửa lại cấu trúc file config)
            _apiKey = emailSettings["AppPassword"]!; 

            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            
            // Header bắt buộc của Brevo API
            _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Cấu trúc JSON payload theo chuẩn API của Brevo
            var payload = new
            {
                sender = new { name = _senderName, email = _senderEmail },
                to = new[] { new { email = toEmail } },
                subject = subject,
                htmlContent = body
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Gửi qua HTTPS (Port 443 - không bao giờ bị khóa trên VPS)
            var response = await _httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                throw new Exception($"Lỗi gửi email qua Brevo API: {response.StatusCode} - {errorMsg}");
            }
        }
    }
}