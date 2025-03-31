using System.Net;
using System.Net.Mail;

namespace backend.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _password;
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _smtpServer = Environment.GetEnvironmentVariable("EMAIL_SMTP_SERVER");
            _fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM");
            _password = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
            
            if (!int.TryParse(Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT"), out _smtpPort))
            {
                _smtpPort = 587; // значение по умолчанию
            }

            _logger = logger;

            if (string.IsNullOrEmpty(_smtpServer) || 
                string.IsNullOrEmpty(_fromEmail) || 
                string.IsNullOrEmpty(_password))
            {
                throw new InvalidOperationException("Email configuration is missing. Please check your environment variables.");
            }
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(new MailAddress(toEmail));

                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    Credentials = new NetworkCredential(_fromEmail, _password),
                    EnableSsl = true
                };

                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                throw new Exception($"Failed to send email: {ex.Message}");
            }
        }
    }
}