using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using System;
using Microsoft.Data.SqlClient;

namespace ark_app1
{
    public class TicketData
    {
        public int SaleId { get; set; }
        public string ClientName { get; set; }
        public string UserName { get; set; }
        public DateTime Date { get; set; }
        public List<TicketItem> Items { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal Total { get; set; }
        public decimal Cash { get; set; }
        public decimal Change { get; set; }
        public string PaymentMethod { get; set; }
    }

    public class TicketItem
    {
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Subtotal { get; set; }
    }

    public static class TicketGenerator
    {
        public static async Task GenerateAndOpenAsync(TicketData data)
        {
            string companyName = "Ticket Venta";
            string companyAddress = null;
            string companyPhone = null;

            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT TOP 1 Nombre, Direccion, Telefono FROM Empresa", conn);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    if (!r.IsDBNull(0)) companyName = r.GetString(0);
                    if (!r.IsDBNull(1)) companyAddress = r.GetString(1);
                    if (!r.IsDBNull(2)) companyPhone = r.GetString(2);
                }
            }
            catch { /* Ignore, use default */ }

            // Create Document
            PdfDocument document = new PdfDocument();
            document.Info.Title = $"Ticket {data.SaleId}";

            PdfPage page = document.AddPage();
            // 80mm width = approx 226 points.
            // Height: Let's guess based on items, or just set long.
            // 1 item ~ 20 points. Header/Footer ~ 200 points.
            double height = 300 + (data.Items.Count * 20);
            page.Width = XUnit.FromMillimeter(80);
            page.Height = XUnit.FromPoint(height);

            XGraphics gfx = XGraphics.FromPdfPage(page);

            // Fonts - PDFsharp uses system fonts by name
            XFont fontTitle = new XFont("Arial", 10, XFontStyle.Bold);
            XFont fontRegular = new XFont("Arial", 8, XFontStyle.Regular);
            XFont fontBold = new XFont("Arial", 8, XFontStyle.Bold);

            double y = 10;
            double width = page.Width.Point;
            double margin = 5;
            double contentWidth = width - (2 * margin);

            // 1. Header
            DrawCenterText(gfx, companyName, fontTitle, width, ref y);
            if (!string.IsNullOrEmpty(companyAddress))
                DrawCenterText(gfx, companyAddress, fontRegular, width, ref y);
            if (!string.IsNullOrEmpty(companyPhone))
                DrawCenterText(gfx, $"Tel: {companyPhone}", fontRegular, width, ref y);

            y += 5;
            DrawCenterText(gfx, "--------------------------------", fontRegular, width, ref y);
            DrawCenterText(gfx, $"Venta #{data.SaleId}", fontBold, width, ref y);
            DrawCenterText(gfx, $"{data.Date:dd/MM/yyyy HH:mm:ss}", fontRegular, width, ref y);
            y += 5;

            // 2. Info
            gfx.DrawString($"Cliente: {data.ClientName}", fontRegular, XBrushes.Black, new XPoint(margin, y)); y += 12;
            gfx.DrawString($"Atendido por: {data.UserName}", fontRegular, XBrushes.Black, new XPoint(margin, y)); y += 12;

            gfx.DrawLine(XPens.Black, margin, y, width - margin, y); y += 3;

            // 3. Grid Header
            // Col widths: Name 50%, Qty 20%, Total 30%
            double col1 = contentWidth * 0.50;
            double col2 = contentWidth * 0.25;
            double col3 = contentWidth * 0.25;

            double x = margin;
            gfx.DrawString("PROD", fontBold, XBrushes.Black, new XPoint(x, y + 8));
            gfx.DrawString("CANT x P", fontBold, XBrushes.Black, new XPoint(x + col1, y + 8));
            gfx.DrawString("TOTAL", fontBold, XBrushes.Black, new XPoint(x + col1 + col2, y + 8));
            y += 12;
            gfx.DrawLine(XPens.Black, margin, y, width - margin, y); y += 3;

            // 4. Items
            foreach (var item in data.Items)
            {
                // Name (Multiline if too long? For simplicity, truncate or wrap manually. PDFsharp doesn't auto wrap easily in DrawString without XTextFormatter)
                // We'll just draw it.
                gfx.DrawString(item.Name, fontRegular, XBrushes.Black, new XRect(x, y, col1, 20), XStringFormats.TopLeft);

                gfx.DrawString($"{item.Quantity:#.##} x {item.Price:#.##}", fontRegular, XBrushes.Black, new XRect(x + col1, y, col2, 20), XStringFormats.TopLeft);

                gfx.DrawString($"{item.Subtotal:N2}", fontRegular, XBrushes.Black, new XRect(x + col1 + col2, y, col3, 20), XStringFormats.TopRight);

                y += 12; // Next row
            }
            y += 5;
            gfx.DrawLine(XPens.Black, margin, y, width - margin, y); y += 5;

            // 5. Totals
            DrawTotalLine(gfx, "Subtotal:", $"{data.Subtotal:N2}", fontRegular, width, margin, ref y);
            if(data.DiscountTotal > 0)
                DrawTotalLine(gfx, "Descuento:", $"-{data.DiscountTotal:N2}", fontRegular, width, margin, ref y);
            DrawTotalLine(gfx, "TOTAL:", $"{data.Total:N2}", fontBold, width, margin, ref y);

            y += 5;
            DrawTotalLine(gfx, $"Pago ({data.PaymentMethod}):", $"{data.Cash:N2}", fontRegular, width, margin, ref y);
            DrawTotalLine(gfx, "Cambio:", $"{data.Change:N2}", fontRegular, width, margin, ref y);

            y += 15;
            DrawCenterText(gfx, "¡Gracias por su compra!", fontBold, width, ref y);

            // Save
            string fileName = $"Ticket_{data.SaleId}_{DateTime.Now.Ticks}.pdf";
            StorageFolder folder = ApplicationData.Current.TemporaryFolder;
            StorageFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            using (var stream = new MemoryStream())
            {
                document.Save(stream, false);
                using (var fileStream = await file.OpenStreamForWriteAsync())
                {
                    stream.Position = 0;
                    await stream.CopyToAsync(fileStream);
                }
            }
            document.Close();

            // Open
            await Launcher.LaunchFileAsync(file);
        }

        private static void DrawCenterText(XGraphics gfx, string text, XFont font, double pageWidth, ref double y)
        {
            XSize size = gfx.MeasureString(text, font);
            gfx.DrawString(text, font, XBrushes.Black, new XPoint((pageWidth - size.Width) / 2, y));
            y += size.Height + 2;
        }

        private static void DrawTotalLine(XGraphics gfx, string label, string value, XFont font, double pageWidth, double margin, ref double y)
        {
            gfx.DrawString(label, font, XBrushes.Black, new XPoint(margin, y));

            XSize sizeVal = gfx.MeasureString(value, font);
            gfx.DrawString(value, font, XBrushes.Black, new XPoint(pageWidth - margin - sizeVal.Width, y));
            y += sizeVal.Height + 2;
        }
    }
}
