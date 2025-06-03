using DinkToPdf;
using DinkToPdf.Contracts;

namespace ParkingApp.Utilities
{
    public class PdfGenerator
    {
        private readonly IConverter _converter;

        public PdfGenerator(IConverter converter)
        {
            _converter = converter;
        }

        public byte[] GeneratePdf(string html)
        {
            var doc = new HtmlToPdfDocument
            {
                GlobalSettings = {
                    PaperSize = PaperKind.A4,
                    Orientation = Orientation.Portrait
                },
                Objects = {
                    new ObjectSettings {
                        HtmlContent = html
                    }
                }
            };

            return _converter.Convert(doc);
        }
    }
}
