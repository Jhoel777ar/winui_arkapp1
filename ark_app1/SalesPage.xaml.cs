using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Data;

namespace ark_app1
{
    public sealed partial class SalesPage : Page
    {
        private ObservableCollection<Producto> _products = new();
        private ObservableCollection<CartItem> _cart = new();

        public SalesPage()
        {
            this.InitializeComponent();
            ProductsGrid.ItemsSource = _products;
            CartList.ItemsSource = _cart;
            Loaded += SalesPage_Loaded;
            _cart.CollectionChanged += (s, e) => CalculateTotal();
        }

        private async void SalesPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadClients();
            await LoadProducts();
            ProductSearchBox.Focus(FocusState.Programmatic);
        }

        private async Task LoadClients()
        {
            try
            {
                var clients = new ObservableCollection<object> { new { Id = (int?)null, Nombre = "Cliente Ocasional" } };
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT Id, Nombre FROM Clientes ORDER BY Nombre", conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    clients.Add(new { Id = r.GetInt32(0), Nombre = r.GetString(1) });
                }
                ClientComboBox.ItemsSource = clients;
                ClientComboBox.SelectedIndex = 0;
            }
            catch (Exception) { ShowInfo("Error", "Error al cargar clientes", InfoBarSeverity.Error); }
        }

        private void NewClientButton_Click(object sender, RoutedEventArgs e)
        {
             this.Frame.Navigate(typeof(ClientsPage));
        }

        private async Task LoadProducts(string filter = "")
        {
            _products.Clear();
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, Codigo, Nombre, Stock, PrecioVenta FROM Productos WHERE Activo = 1";
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    cmd.CommandText += " AND (Nombre LIKE @f OR Codigo LIKE @f)";
                    cmd.Parameters.AddWithValue("@f", $"%{filter}%");
                }

                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    _products.Add(new Producto
                    {
                        Id = r.GetInt32(0),
                        Codigo = r.GetString(1),
                        Nombre = r.GetString(2),
                        Stock = r.GetDecimal(3),
                        PrecioVenta = r.GetDecimal(4)
                    });
                }
            }
            catch (Exception ex)
            {
                ShowInfo("Error", "Error al cargar productos: " + ex.Message, InfoBarSeverity.Error);
            }
        }

        private void ProductSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ = LoadProducts(ProductSearchBox.Text);
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: Producto p })
            {
                if (p.Stock <= 0)
                {
                    ShowInfo("Stock", "Producto sin stock", InfoBarSeverity.Warning);
                    return;
                }

                var existing = _cart.FirstOrDefault(c => c.ProductoId == p.Id);
                if (existing != null)
                {
                    if (existing.Cantidad + 1 > p.Stock)
                    {
                        ShowInfo("Stock", "No hay suficiente stock", InfoBarSeverity.Warning);
                        return;
                    }
                    existing.Cantidad++;
                }
                else
                {
                    _cart.Add(new CartItem
                    {
                        ProductoId = p.Id,
                        Nombre = p.Nombre,
                        PrecioUnitario = p.PrecioVenta,
                        Cantidad = 1,
                        StockMax = p.Stock
                    });
                }
                CalculateTotal();
            }
        }

        private void CartQty_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (sender.DataContext is CartItem item)
            {
                if (args.NewValue > (double)item.StockMax)
                {
                    sender.Value = (double)item.StockMax; // Revert
                    ShowInfo("Stock", $"Solo hay {item.StockMax} en stock", InfoBarSeverity.Warning);
                }
                else
                {
                    item.Cantidad = (decimal)args.NewValue;
                    CalculateTotal();
                }
            }
        }

        private void CalculateTotal()
        {
            decimal total = _cart.Sum(x => x.Subtotal);
            TotalText.Text = $"Bs. {total:N2}";
            CalculateChange();
        }

        private void EfectivoBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            CalculateChange();
        }

        private void CalculateChange()
        {
            try
            {
                decimal total = _cart.Sum(x => x.Subtotal);
                decimal efectivo = 0;

                if (double.IsFinite(EfectivoBox.Value) && EfectivoBox.Value < (double)decimal.MaxValue)
                {
                    efectivo = (decimal)EfectivoBox.Value;
                }

                if (efectivo >= total && total > 0)
                {
                    CambioText.Text = $"Cambio: Bs. {(efectivo - total):N2}";
                    CambioText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                }
                else
                {
                    CambioText.Text = "Cambio: Bs. 0.00";
                    CambioText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
                }
            }
            catch
            {
                CambioText.Text = "Error";
            }
        }

        private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Auto-add on selection or show details
        }

        private void ClearCart_Click(object sender, RoutedEventArgs e)
        {
            _cart.Clear();
            EfectivoBox.Value = 0;
            CalculateTotal();
        }

        private async void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_cart.Any())
            {
                ShowInfo("Carrito vacío", "Agregue productos para vender", InfoBarSeverity.Warning);
                return;
            }

            decimal total = _cart.Sum(x => x.Subtotal);
            if (EfectivoBox.Value < (double)total)
            {
                ShowInfo("Pago insuficiente", "El efectivo recibido es menor al total", InfoBarSeverity.Error);
                return;
            }

            var json = JsonSerializer.Serialize(_cart.Select(c => new
            {
                c.ProductoId,
                c.Cantidad,
                c.PrecioUnitario,
                DescuentoPorcentaje = 0,
                DescuentoMonto = 0
            }));

            try
            {
                int userId = (Application.Current as App)?.CurrentUser?.Id ?? 1;

                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
                var cmd = new SqlCommand("sp_RegistrarVenta", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@UsuarioId", userId);
                cmd.Parameters.AddWithValue("@ClienteId", ClientComboBox.SelectedValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Productos", json);
                cmd.Parameters.AddWithValue("@EfectivoRecibido", (decimal)EfectivoBox.Value);
                cmd.Parameters.AddWithValue("@TipoPago", "Efectivo");

                var pRes = cmd.Parameters.Add("@Resultado", SqlDbType.Bit); pRes.Direction = ParameterDirection.Output;
                var pMsg = cmd.Parameters.Add("@Mensaje", SqlDbType.NVarChar, 500); pMsg.Direction = ParameterDirection.Output;
                var pId = cmd.Parameters.Add("@VentaId", SqlDbType.Int); pId.Direction = ParameterDirection.Output;

                await cmd.ExecuteNonQueryAsync();

                bool success = (bool)pRes.Value;
                string msg = pMsg.Value.ToString();

                if (success)
                {
                    ShowInfo("Venta Exitosa", msg, InfoBarSeverity.Success);
                    _cart.Clear();
                    EfectivoBox.Value = 0;
                    CalculateTotal();
                    await LoadProducts(ProductSearchBox.Text); // Refresh stock
                }
                else
                {
                    ShowInfo("Error", msg, InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                ShowInfo("Error Crítico", ex.Message, InfoBarSeverity.Error);
            }
        }

        private void ShowInfo(string title, string msg, InfoBarSeverity severity)
        {
            CartInfoBar.Title = title;
            CartInfoBar.Message = msg ?? string.Empty;
            CartInfoBar.Severity = severity;
            CartInfoBar.IsOpen = true;
        }
    }

    public class CartItem : INotifyPropertyChanged
    {
        public int ProductoId { get; set; }
        public required string Nombre { get; set; }
        public decimal PrecioUnitario { get; set; }

        private decimal _cantidad;
        public decimal Cantidad
        {
            get => _cantidad;
            set
            {
                _cantidad = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Subtotal));
            }
        }

        public decimal StockMax { get; set; }

        public decimal Subtotal => PrecioUnitario * Cantidad;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
