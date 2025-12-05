using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage.Pickers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Windows.Storage;
using Windows.System;
using System.IO;

namespace ark_app1
{
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();
            Loaded += async (s, e) => await LoadDashboardData();
        }

        private async Task LoadDashboardData()
        {
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
                var cmd = new SqlCommand("sp_ObtenerDatosReporte", conn);
                using var r = await cmd.ExecuteReaderAsync();

                // 1. Chart Data
                var salesBars = new List<ChartBar>();
                var profitBars = new List<ChartBar>();
                decimal maxSale = 1;
                decimal maxProfit = 1;

                while (await r.ReadAsync())
                {
                    DateTime date = r.GetDateTime(0);
                    decimal sale = r.GetDecimal(1);
                    decimal profit = r.GetDecimal(2);

                    if (sale > maxSale) maxSale = sale;
                    if (profit > maxProfit) maxProfit = profit;

                    string label = date.ToString("dd/MM");

                    salesBars.Add(new ChartBar { Label = label, RawValue = sale, Tooltip = $"Ventas {label}: {sale:C2}" });
                    profitBars.Add(new ChartBar { Label = label, RawValue = profit, Tooltip = $"Ganancia {label}: {profit:C2}" });
                }

                // Normalize heights (Max 150px)
                foreach (var b in salesBars) b.Height = (double)(b.RawValue / maxSale) * 150;
                foreach (var b in profitBars) b.Height = (double)(b.RawValue / maxProfit) * 150;

                SalesChart.ItemsSource = salesBars;
                ProfitChart.ItemsSource = profitBars;

                // 2. Low Stock (Skip for dashboard UI, used for report)
                await r.NextResultAsync();

                // 3. Recent Sales (Skip for dashboard UI)
                await r.NextResultAsync();

                // 4. Totals
                if (await r.NextResultAsync() && await r.ReadAsync())
                {
                    TxtTotalProducts.Text = r.GetInt32(0).ToString();
                    TxtTotalSales.Text = r.IsDBNull(1) ? "Bs. 0.00" : $"Bs. {r.GetDecimal(1):N2}";
                    TxtTotalClients.Text = r.GetInt32(2).ToString();
                }
            }
            catch (Exception ex)
            {
                ShowInfo("Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        private async void EditCompany_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = "Configurar Empresa",
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary
            };

            var stack = new StackPanel { Spacing = 10, Width = 350 };
            var txtNombre = new TextBox { Header = "Nombre Empresa", PlaceholderText = "Requerido" };
            var txtTel = new TextBox { Header = "Teléfono" };
            var txtTel2 = new TextBox { Header = "Teléfono Secundario" };
            var txtDir = new TextBox { Header = "Dirección" };
            var txtEmail = new TextBox { Header = "Email" };
            var txtWeb = new TextBox { Header = "Sitio Web" };

            // Load existing
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT TOP 1 Nombre, Telefono, TelefonoSecundario, Direccion, Email, SitioWeb FROM Empresa", conn);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    txtNombre.Text = r.GetString(0);
                    if(!r.IsDBNull(1)) txtTel.Text = r.GetString(1);
                    if(!r.IsDBNull(2)) txtTel2.Text = r.GetString(2);
                    if(!r.IsDBNull(3)) txtDir.Text = r.GetString(3);
                    if(!r.IsDBNull(4)) txtEmail.Text = r.GetString(4);
                    if(!r.IsDBNull(5)) txtWeb.Text = r.GetString(5);
                }
            }
            catch { }

            stack.Children.Add(txtNombre);
            stack.Children.Add(txtTel);
            stack.Children.Add(txtTel2);
            stack.Children.Add(txtDir);
            stack.Children.Add(txtEmail);
            stack.Children.Add(txtWeb);
            dialog.Content = stack;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                if (string.IsNullOrWhiteSpace(txtNombre.Text))
                {
                    ShowInfo("Error", "El nombre es obligatorio", InfoBarSeverity.Error);
                    return;
                }

                try
                {
                    using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                    await conn.OpenAsync();
                    var cmd = new SqlCommand("sp_GestionarEmpresa", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Nombre", txtNombre.Text);
                    cmd.Parameters.AddWithValue("@Telefono", txtTel.Text);
                    cmd.Parameters.AddWithValue("@TelefonoSecundario", txtTel2.Text);
                    cmd.Parameters.AddWithValue("@Direccion", txtDir.Text);
                    cmd.Parameters.AddWithValue("@Email", txtEmail.Text);
                    cmd.Parameters.AddWithValue("@SitioWeb", txtWeb.Text);

                    var pRes = new SqlParameter("@Resultado", System.Data.SqlDbType.Bit) { Direction = System.Data.ParameterDirection.Output };
                    var pMsg = new SqlParameter("@Mensaje", System.Data.SqlDbType.NVarChar, 500) { Direction = System.Data.ParameterDirection.Output };
                    cmd.Parameters.Add(pRes);
                    cmd.Parameters.Add(pMsg);

                    await cmd.ExecuteNonQueryAsync();
                    ShowInfo("Éxito", pMsg.Value.ToString(), InfoBarSeverity.Success);
                }
                catch (Exception ex)
                {
                    ShowInfo("Error", ex.Message, InfoBarSeverity.Error);
                }
            }
        }

        private async void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("PDF Document", new List<string>() { ".pdf" });
            picker.SuggestedFileName = $"Reporte_General_{DateTime.Now:yyyyMMdd}";

            // WinUI 3 Window Handle hack (Standard)
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file == null) return;

            try
            {
                // Fetch All Data
                var lowStock = new List<dynamic>();
                var recentSales = new List<dynamic>();

                using (var conn = new SqlConnection(DatabaseManager.ConnectionString))
                {
                    await conn.OpenAsync();
                    var cmd = new SqlCommand("sp_ObtenerDatosReporte", conn);
                    using var r = await cmd.ExecuteReaderAsync();

                    // 1. Skip Charts
                    await r.NextResultAsync();

                    // 2. Low Stock
                    while(await r.ReadAsync())
                    {
                        lowStock.Add(new { Codigo = r.GetString(0), Nombre = r.GetString(1), Stock = r.GetDecimal(2), Min = r.GetDecimal(3) });
                    }
                    await r.NextResultAsync();

                    // 3. Sales
                    while (await r.ReadAsync())
                    {
                        recentSales.Add(new { Id = r.GetInt32(0), Fecha = r.GetDateTime(1), Cliente = r.IsDBNull(2)?"":r.GetString(2), Total = r.GetDecimal(3), Estado = r.GetString(4) });
                    }
                }

                // Generate PDF
                QuestPDF.Settings.License = LicenseType.Community;

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(1, Unit.Centimeter);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Reporte General del Sistema").FontSize(20).Bold();
                                col.Item().Text($"Generado: {DateTime.Now}").FontSize(10);
                            });
                        });

                        page.Content().Column(col =>
                        {
                            col.Item().PaddingTop(10).Text("Resumen de Inventario (Bajo Stock)").FontSize(14).Bold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.RelativeColumn(3);
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });
                                table.Header(h => { h.Cell().Text("COD"); h.Cell().Text("Producto"); h.Cell().Text("Stock"); h.Cell().Text("Min"); });
                                foreach(var item in lowStock)
                                {
                                    table.Cell().Text(item.Codigo);
                                    table.Cell().Text(item.Nombre);
                                    table.Cell().Text(item.Stock.ToString());
                                    table.Cell().Text(item.Min.ToString());
                                }
                            });

                            col.Item().PaddingTop(20).Text("Últimas Ventas").FontSize(14).Bold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });
                                table.Header(h => { h.Cell().Text("ID"); h.Cell().Text("Fecha"); h.Cell().Text("Cliente"); h.Cell().Text("Total"); h.Cell().Text("Estado"); });
                                foreach (var item in recentSales)
                                {
                                    table.Cell().Text(item.Id.ToString());
                                    table.Cell().Text(item.Fecha.ToString("g"));
                                    table.Cell().Text(item.Cliente);
                                    table.Cell().Text($"{item.Total:N2}");
                                    table.Cell().Text(item.Estado);
                                }
                            });
                        });

                        page.Footer().AlignCenter().Text(x => x.CurrentPageNumber());
                    });
                }).GeneratePdf(file.Path); // WinUI can write to PickedFile path usually if app has rights or via stream

                // Open
                await Launcher.LaunchFileAsync(file);
                ShowInfo("Reporte Generado", "El reporte se ha guardado y abierto.", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowInfo("Error", "No se pudo generar el reporte: " + ex.Message, InfoBarSeverity.Error);
            }
        }

        private void ShowInfo(string title, string msg, InfoBarSeverity severity)
        {
            ResultInfoBar.Title = title;
            ResultInfoBar.Message = msg;
            ResultInfoBar.Severity = severity;
            ResultInfoBar.IsOpen = true;
        }
    }

    public class ChartBar
    {
        public string Label { get; set; }
        public decimal RawValue { get; set; }
        public double Height { get; set; }
        public string Tooltip { get; set; }
    }
}
