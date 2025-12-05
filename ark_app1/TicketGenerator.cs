using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using System;

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
            // Create Document
            using (PdfDocument doc = new PdfDocument())
            {
                // 80mm width approx 226 points. Margins 0 for max space.
                doc.PageSettings.Margins.All = 0;
                // Height is fixed in PDF usually, setting a long strip
                doc.PageSettings.Size = new SizeF(226, 800);

                PdfPage page = doc.Pages.Add();
                PdfGraphics graphics = page.Graphics;

                // Fonts
                PdfFont fontTitle = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
                PdfFont fontRegular = new PdfStandardFont(PdfFontFamily.Helvetica, 8);
                PdfFont fontBold = new PdfStandardFont(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);

                float y = 10; // Start with some margin
                float width = page.GetClientSize().Width;

                // Header
                DrawCenterText(graphics, "ARK SALES", fontTitle, width, ref y);
                DrawCenterText(graphics, "Ticket de Venta", fontRegular, width, ref y);
                DrawCenterText(graphics, $"#{data.SaleId} - {data.Date:dd/MM/yy HH:mm}", fontRegular, width, ref y);
                y += 5;

                // Info
                graphics.DrawString($"Cliente: {data.ClientName}", fontRegular, PdfBrushes.Black, new PointF(5, y)); y += 10;
                graphics.DrawString($"Atendido: {data.UserName}", fontRegular, PdfBrushes.Black, new PointF(5, y)); y += 15;

                // Grid
                PdfGrid grid = new PdfGrid();
                grid.Columns.Add(3);
                grid.Headers.Add(1);

                PdfGridRow header = grid.Headers[0];
                header.Cells[0].Value = "Prod";
                header.Cells[1].Value = "Cant x P";
                header.Cells[2].Value = "Tot";

                // Adjust widths (Total 226)
                grid.Columns[0].Width = 100;
                grid.Columns[1].Width = 70;
                grid.Columns[2].Width = 56;

                // Style
                grid.Style.Font = fontRegular;
                grid.Style.CellPadding.All = 2;
                grid.Headers.ApplyStyle(new PdfGridCellStyle { Font = fontBold, Borders = new PdfBorders { All = PdfPens.Transparent, Bottom = PdfPens.Black } });

                foreach(var item in data.Items)
                {
                    PdfGridRow row = grid.Rows.Add();
                    row.Cells[0].Value = item.Name;
                    row.Cells[1].Value = $"{item.Quantity:#.##} x {item.Price:#.##}";
                    row.Cells[2].Value = $"{item.Subtotal:N2}";

                    // Simple borders
                    foreach(PdfGridCell cell in row.Cells)
                    {
                        cell.Style.Borders.All = PdfPens.Transparent;
                    }
                }

                PdfGridLayoutResult result = grid.Draw(page, new PointF(0, y));
                y = result.Bounds.Bottom + 10;

                // Totals
                DrawRightText(graphics, $"Subtotal: {data.Subtotal:N2}", fontRegular, width, ref y);
                if(data.DiscountTotal > 0)
                    DrawRightText(graphics, $"Descuento: -{data.DiscountTotal:N2}", fontRegular, width, ref y);
                DrawRightText(graphics, $"TOTAL: {data.Total:N2}", fontBold, width, ref y);

                y += 5;
                DrawRightText(graphics, $"Pago ({data.PaymentMethod}): {data.Cash:N2}", fontRegular, width, ref y);
                DrawRightText(graphics, $"Cambio: {data.Change:N2}", fontRegular, width, ref y);

                y += 15;
                DrawCenterText(graphics, "¡Gracias por su compra!", fontBold, width, ref y);

                // Save
                string fileName = $"Ticket_{data.SaleId}_{DateTime.Now.Ticks}.pdf";
                StorageFolder folder = ApplicationData.Current.TemporaryFolder;
                StorageFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

                using (var stream = await file.OpenStreamForWriteAsync())
                {
                    doc.Save(stream);
                }

                // Open
                await Launcher.LaunchFileAsync(file);
            }
        }

        private static void DrawCenterText(PdfGraphics g, string text, PdfFont font, float width, ref float y)
        {
            SizeF size = font.MeasureString(text);
            g.DrawString(text, font, PdfBrushes.Black, new PointF((width - size.Width) / 2, y));
            y += size.Height + 2;
        }

        private static void DrawRightText(PdfGraphics g, string text, PdfFont font, float width, ref float y)
        {
            SizeF size = font.MeasureString(text);
            g.DrawString(text, font, PdfBrushes.Black, new PointF(width - size.Width - 5, y));
            y += size.Height + 2;
        }
    }
}
