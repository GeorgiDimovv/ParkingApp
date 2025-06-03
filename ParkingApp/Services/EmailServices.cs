using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace ParkingApp.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string content, byte[] pdfBytes, string fileName)
        {
            var client = new SendGridClient("YOUR_SENDGRID_API_KEY");
            var from = new EmailAddress("your@email.com", "Parking Admin");
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, content, content);

            var pdfBase64 = Convert.ToBase64String(pdfBytes);
            msg.AddAttachment(fileName, pdfBase64, "application/pdf");

            await client.SendEmailAsync(msg);
        }

    }
}