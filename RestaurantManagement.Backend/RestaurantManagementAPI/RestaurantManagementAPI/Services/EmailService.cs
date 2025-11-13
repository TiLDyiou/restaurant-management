using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace RestaurentManagementAPI.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly string _appPassword;
        private readonly string _host;
        private readonly int _port;

        public EmailService(IConfiguration config)
        {
            _config = config;

            var emailSettings = _config.GetSection("EmailSettings");
            _senderEmail = emailSettings["SenderEmail"] ?? throw new ArgumentNullException("SenderEmail is not configured.");
            _senderName = emailSettings["SenderName"] ?? "NoName";
            _appPassword = emailSettings["AppPassword"] ?? throw new ArgumentNullException("AppPassword is not configured.");
            _host = emailSettings["Host"] ?? "smtp.gmail.com";
            _port = int.TryParse(emailSettings["Port"], out int port) ? port : 587; // mặc định port 587
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Recipient email is required.", nameof(toEmail));

            using var mail = new MailMessage()
            {
                From = new MailAddress(_senderEmail, _senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(new MailAddress(toEmail));

            using var smtp = new SmtpClient(_host, _port)
            {
                Credentials = new NetworkCredential(_senderEmail, _appPassword),
                EnableSsl = true
            };

            await smtp.SendMailAsync(mail);
        }
    }
}
