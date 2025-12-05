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
        private int? _selectedClientId = null;

        public SalesPage()
        {
            this.InitializeComponent();
            ProductsGrid.ItemsSource = _products;
            CartGrid.ItemsSource = _cart;
            Loaded += SalesPage_Loaded;
            _cart.CollectionChanged += (s, e) => CalculateTotal();
        }

        private async void SalesPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProducts();
            ProductSearchBox.Focus(FocusState.Programmatic);
        }

        // --- Client Search Logic ---
        private async void ClientSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (string.IsNullOrWhiteSpace(sender.Text))
                {
                    sender.ItemsSource = null;
                    return;
                }

                try
                {
                    var suggestions = new ObservableCollection<ClientSearchResult>();
                    using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                    await conn.OpenAsync();
                    var cmd = new SqlCommand("SELECT TOP 10 Id, Nombre FROM Clientes WHERE Nombre LIKE @f ORDER BY Nombre", conn);
                    cmd.Parameters.AddWithValue("@f", $"%{sender.Text}%");
                    using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        suggestions.Add(new ClientSearchResult { Id = r.GetInt32(0), Nombre = r.GetString(1) });
                    }
                    sender.ItemsSource = suggestions;
                }
                catch { /* Ignore */ }
            }
        }

        private void ClientSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is ClientSearchResult client)
            {
                _selectedClientId = client.Id;
                SelectedClientText.Text = $"Cliente: {client.Nombre}";
            }
        }

        private void ClientSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion == null)
            {
                if (string.IsNullOrWhiteSpace(args.QueryText))
                {
                    _selectedClientId = null;
                    SelectedClientText.Text = "Cliente: Ocasional";
                }
                else
                {
                    _selectedClientId = null;
                    SelectedClientText.Text = "Cliente: Ocasional (No registrado)";
                }
            }
        }

        private void NewClientButton_Click(object sender, RoutedEventArgs e)
        {
             this.Frame.Navigate(typeof(ClientsPage));
        }

        // --- Product Logic ---
        private async Task LoadProducts(string filter = "")
        {
            _products.Clear();
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, Codigo, Nombre, Stock, PrecioVenta FROM Productos WHERE Activo = 1 AND Stock > 0";
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

        // --- Cart Logic ---
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

        private void RemoveFromCart_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: CartItem item })
            {
                _cart.Remove(item);
            }
        }

        private void CartQty_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (sender.DataContext is CartItem item)
            {
                double val = args.NewValue;
                // Handle NaN or invalid
                if (double.IsNaN(val) || val < 1) val = 1;

                // Validation against max stock
                if (val > (double)item.StockMax)
                {
                    sender.Value = (double)item.StockMax;
                    ShowInfo("Stock", $"Stock máximo disponible: {item.StockMax}", InfoBarSeverity.Warning);
                    item.Cantidad = item.StockMax;
                }
                else
                {
                    item.Cantidad = (decimal)val;
                    CalculateTotal();
                }
            }
        }

        private void Discount_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            CalculateTotal();
        }

        private void CalculateTotal()
        {
            if (SubtotalText == null) return;

            decimal subtotal = _cart.Sum(x => x.Subtotal);

            // Safe casting
            double dPercent = DiscountPercentBox.Value;
            decimal discountPercent = double.IsNaN(dPercent) ? 0 : (decimal)dPercent;

            double dAmount = DiscountAmountBox.Value;
            decimal discountAmount = double.IsNaN(dAmount) ? 0 : (decimal)dAmount;

            decimal totalDiscount = 0;

            decimal tempTotal = subtotal;
            if (discountAmount > 0) tempTotal -= discountAmount;
            if (discountPercent > 0) tempTotal -= (tempTotal * discountPercent / 100);

            totalDiscount = subtotal - tempTotal;
            if (totalDiscount < 0) totalDiscount = 0;

            SubtotalText.Text = $"Bs. {subtotal:N2}";
            DiscountText.Text = $"- Bs. {totalDiscount:N2}";
            TotalText.Text = $"Bs. {tempTotal:N2}";
        }

        private void ClearCart_Click(object sender, RoutedEventArgs e)
        {
            _cart.Clear();
            DiscountAmountBox.Value = 0;
            DiscountPercentBox.Value = 0;
            CalculateTotal();
            ClientSearchBox.Text = "";
            _selectedClientId = null;
            SelectedClientText.Text = "Cliente: Ocasional";
        }

        // --- Checkout Logic ---
        private async void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_cart.Any())
            {
                ShowInfo("Carrito vacío", "Agregue productos para vender", InfoBarSeverity.Warning);
                return;
            }

            decimal subtotal = _cart.Sum(x => x.Subtotal);

            double dPercent = DiscountPercentBox.Value;
            decimal discountPercent = double.IsNaN(dPercent) ? 0 : (decimal)dPercent;

            double dAmount = DiscountAmountBox.Value;
            decimal discountAmount = double.IsNaN(dAmount) ? 0 : (decimal)dAmount;

            decimal totalToPay = subtotal;
            if (discountAmount > 0) totalToPay -= discountAmount;
            if (discountPercent > 0) totalToPay -= (totalToPay * discountPercent / 100);
            if (totalToPay < 0) totalToPay = 0;

            var dialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = "Finalizar Venta",
                PrimaryButtonText = "Confirmar Pago",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary
            };

            var stack = new StackPanel { Spacing = 10 };
            stack.Children.Add(new TextBlock { Text = $"Total a Pagar: Bs. {totalToPay:N2}", FontSize = 24, FontWeight = Microsoft.UI.Text.FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });

            var cashBox = new NumberBox
            {
                Header = "Efectivo Recibido (Bs.)",
                PlaceholderText = "Ingrese monto",
                Minimum = 0,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                Value = (double)totalToPay
            };

            var changeText = new TextBlock { Text = "Cambio: Bs. 0.00", FontSize = 18, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) };

            cashBox.ValueChanged += (s, args) =>
            {
                double val = args.NewValue;
                // Check NaN
                if (double.IsNaN(val)) val = 0;

                if (val >= (double)totalToPay)
                {
                    changeText.Text = $"Cambio: Bs. {(val - (double)totalToPay):N2}";
                    changeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                    dialog.IsPrimaryButtonEnabled = true;
                }
                else
                {
                    changeText.Text = "Monto insuficiente";
                    changeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                    dialog.IsPrimaryButtonEnabled = false;
                }
            };

            stack.Children.Add(cashBox);
            stack.Children.Add(changeText);
            dialog.Content = stack;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await ProcessSale((decimal)cashBox.Value, totalToPay);
            }
        }

        private async Task ProcessSale(decimal efectivoRecibido, decimal totalEsperado)
        {
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
                var cmd = new SqlCommand("sp_RegistrarVenta_v2", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                double dPercent = DiscountPercentBox.Value;
                decimal discountPercent = double.IsNaN(dPercent) ? 0 : (decimal)dPercent;

                double dAmount = DiscountAmountBox.Value;
                decimal discountAmount = double.IsNaN(dAmount) ? 0 : (decimal)dAmount;

                cmd.Parameters.AddWithValue("@UsuarioId", userId);
                cmd.Parameters.AddWithValue("@ClienteId", (object)_selectedClientId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Productos", json);
                cmd.Parameters.AddWithValue("@EfectivoRecibido", efectivoRecibido);
                cmd.Parameters.AddWithValue("@TipoPago", "Efectivo");
                cmd.Parameters.AddWithValue("@DescuentoGlobalPorcentaje", discountPercent);
                cmd.Parameters.AddWithValue("@DescuentoGlobalMonto", discountAmount);

                var pRes = cmd.Parameters.Add("@Resultado", SqlDbType.Bit); pRes.Direction = ParameterDirection.Output;
                var pMsg = cmd.Parameters.Add("@Mensaje", SqlDbType.NVarChar, 500); pMsg.Direction = ParameterDirection.Output;
                var pId = cmd.Parameters.Add("@VentaId", SqlDbType.Int); pId.Direction = ParameterDirection.Output;

                await cmd.ExecuteNonQueryAsync();

                bool success = (bool)pRes.Value;
                string msg = pMsg.Value.ToString();

                if (success)
                {
                    ShowInfo("Venta Exitosa", msg, InfoBarSeverity.Success);
                    ClearCart_Click(null, null);
                    await LoadProducts(ProductSearchBox.Text);
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
                if (_cantidad != value)
                {
                    _cantidad = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Subtotal));
                    OnPropertyChanged(nameof(SubtotalDisplay));
                }
            }
        }

        public decimal StockMax { get; set; }

        public decimal Subtotal => PrecioUnitario * Cantidad;
        public string SubtotalDisplay => $"Bs. {Subtotal:N2}";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ClientSearchResult
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public override string ToString() => Nombre;
    }
}
