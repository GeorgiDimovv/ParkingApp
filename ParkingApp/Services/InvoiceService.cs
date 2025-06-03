using ParkingApp.Data.Models;
using ParkingApp.Utilities;

namespace ParkingApp.Services
{
    public class InvoiceService
    {
        private readonly PdfGenerator _pdfGenerator;

        public InvoiceService(PdfGenerator pdfGenerator)
        {
            _pdfGenerator = pdfGenerator;
        }

        public string PopulateTemplate(Subscriber sub, string invoiceNumber)
        {
            var templatePath = Path.Combine("Templates", "InvoiceTemplate.html");
            var html = File.ReadAllText(templatePath);

            return html
                .Replace("{{InvoiceNumber}}", invoiceNumber)
                .Replace("{{InvoiceDate}}", DateTime.Now.ToString("dd.MM.yyyy"))
                .Replace("{{SubscriberName}}", sub.Name)
                //.Replace("{{SubscriberAddress}}", sub.Address ?? "—")
                .Replace("{{SubscriberEmail}}", sub.EmailAddress)
                .Replace("{{UnitPrice}}", $"{sub.TotalPriceInBgn:0.00}")
                .Replace("{{TotalPrice}}", $"{sub.TotalPriceInBgn:0.00}");
                //.Replace("{{FinalAmount}}", $"{sub.TotalPriceInBgn  1.20:0.00}");
        }

        public byte[] GenerateInvoicePdf(Subscriber sub)
        {
            var invoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{sub.Id}";
            var html = PopulateTemplate(sub, invoiceNumber);
            return _pdfGenerator.GeneratePdf(html);
        }
    }
}
