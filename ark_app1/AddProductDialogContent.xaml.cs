using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Windows.Graphics;

namespace ark_app1
{
    public class ProductoTemp
    {
        public required string Codigo { get; set; }
        public required string Nombre { get; set; }
        public int? CategoriaId { get; set; }
        public string? Talla { get; set; }
        public string? Color { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal Cantidad { get; set; }
        public string? UnidadMedida { get; set; } = "Unidad";
        public decimal StockMinimo { get; set; } = 5;
        public decimal Subtotal => Cantidad * PrecioCompra;
    }

    public sealed partial class AddCompraDialog : Window
    {
        private readonly ObservableCollection<ProductoTemp> _productos = new();
        private readonly bool _editMode;
        private readonly int? _compraId;

        public AddCompraDialog()
        {
            InitializeComponent();
            this.Title = "Registrar Compra";
            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
            CenterWindow();
            ProductosListView.ItemsSource = _productos;
            CargarDatos();
            var presenter = AppWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
            }
        }

        public AddCompraDialog(int compraId) : this()
        {
            _editMode = true;
            _compraId = compraId;
            TitleTextBlock.Text = $"Editar Compra #{compraId}";
            SaveButton.Content = "Guardar Cambios";
        }

        private void CenterWindow()
        {
            var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            var screenWidth = displayArea.WorkArea.Width;
            var screenHeight = displayArea.WorkArea.Height;
            var windowWidth = 1200;
            var windowHeight = 970;
            AppWindow.Resize(new SizeInt32(windowWidth, windowHeight));
            AppWindow.Move(new PointInt32((screenWidth - windowWidth) / 2, (screenHeight - windowHeight) / 2));
        }

        private async void CargarDatos()
        {
            await CargarProveedores();
            await CargarCategorias();
        }

        private async Task CargarProveedores()
        {
            var items = new ObservableCollection<object> { new { Id = (int?)null, Nombre = "(Sin proveedor)" } };
            using var conn = new SqlConnection(DatabaseManager.ConnectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand("SELECT Id, Nombre FROM Proveedores ORDER BY Nombre", conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                items.Add(new { Id = r.GetInt32(0), Nombre = r.GetString(1) });
            ProveedorComboBox.ItemsSource = items;
            ProveedorComboBox.DisplayMemberPath = "Nombre";
            ProveedorComboBox.SelectedValuePath = "Id";
            ProveedorComboBox.SelectedIndex = 0;
        }

        private async Task CargarCategorias()
        {
            var items = new ObservableCollection<object>();
            using var conn = new SqlConnection(DatabaseManager.ConnectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand("SELECT Id, Nombre FROM Categorias ORDER BY Nombre", conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                items.Add(new { Id = r.GetInt32(0), Nombre = r.GetString(1) });
            CategoriaComboBox.ItemsSource = items;
            CategoriaComboBox.DisplayMemberPath = "Nombre";
            CategoriaComboBox.SelectedValuePath = "Id";
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CodigoTextBox.Text) || CodigoTextBox.Text.Trim().Length > 50)
            {
                MostrarInfo("Código obligatorio, máx. 50 chars", InfoBarSeverity.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(NombreTextBox.Text) || NombreTextBox.Text.Trim().Length > 150)
            {
                MostrarInfo("Nombre obligatorio, máx. 150 chars", InfoBarSeverity.Error);
                return;
            }
            if (PrecioCompraNumberBox.Value <= 0)
            {
                MostrarInfo("Precio compra > 0", InfoBarSeverity.Error);
                return;
            }
            if (PrecioVentaNumberBox.Value <= 0)
            {
                MostrarInfo("Precio venta > 0", InfoBarSeverity.Error);
                return;
            }
            if (CantidadNumberBox.Value <= 0)
            {
                MostrarInfo("Cantidad > 0", InfoBarSeverity.Error);
                return;
            }
            if (!string.IsNullOrWhiteSpace(TallaTextBox.Text) && TallaTextBox.Text.Length > 20)
            {
                MostrarInfo("Talla máx. 20 chars", InfoBarSeverity.Error);
                return;
            }
            if (!string.IsNullOrWhiteSpace(ColorTextBox.Text) && ColorTextBox.Text.Length > 30)
            {
                MostrarInfo("Color máx. 30 chars", InfoBarSeverity.Error);
                return;
            }
            if (!string.IsNullOrWhiteSpace(UnidadMedidaTextBox.Text) && UnidadMedidaTextBox.Text.Length > 20)
            {
                MostrarInfo("Unidad máx. 20 chars", InfoBarSeverity.Error);
                return;
            }

            var stockMinimo = StockMinimoNumberBox.Value > 0 ? (decimal)StockMinimoNumberBox.Value : 5m;

            _productos.Add(new ProductoTemp
            {
                Codigo = CodigoTextBox.Text.Trim(),
                Nombre = NombreTextBox.Text.Trim(),
                CategoriaId = CategoriaComboBox.SelectedValue as int?,
                Talla = string.IsNullOrWhiteSpace(TallaTextBox.Text) ? null : TallaTextBox.Text.Trim(),
                Color = string.IsNullOrWhiteSpace(ColorTextBox.Text) ? null : ColorTextBox.Text.Trim(),
                PrecioCompra = (decimal)PrecioCompraNumberBox.Value,
                PrecioVenta = (decimal)PrecioVentaNumberBox.Value,
                Cantidad = (decimal)CantidadNumberBox.Value,
                UnidadMedida = string.IsNullOrWhiteSpace(UnidadMedidaTextBox.Text) ? null : UnidadMedidaTextBox.Text.Trim(),
                StockMinimo = stockMinimo
            });

            LimpiarCampos();
            MostrarInfo("Producto agregado correctamente", InfoBarSeverity.Success);
        }

        private void RemoveProduct_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ProductoTemp p)
                _productos.Remove(p);
        }

        private void LimpiarCampos()
        {
            CodigoTextBox.Text = "";
            NombreTextBox.Text = "";
            TallaTextBox.Text = "";
            ColorTextBox.Text = "";
            UnidadMedidaTextBox.Text = "";
            PrecioCompraNumberBox.Value = 0;
            PrecioVentaNumberBox.Value = 0;
            CantidadNumberBox.Value = 1;
            StockMinimoNumberBox.Value = 5;
            CategoriaComboBox.SelectedIndex = -1;
            CodigoTextBox.Focus(FocusState.Programmatic);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_productos.Any())
            {
                MostrarInfo("Agregue al menos un producto", InfoBarSeverity.Warning);
                return;
            }

            var json = JsonSerializer.Serialize(_productos.Select(p => new
            {
                p.Codigo,
                p.Nombre,
                p.CategoriaId,
                Talla = p.Talla,
                Color = p.Color,
                p.PrecioCompra,
                p.PrecioVenta,
                p.Cantidad,
                UnidadMedida = p.UnidadMedida,
                p.StockMinimo
            }));

            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(_editMode ? "sp_EditarCompra" : "sp_RegistrarCompra", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                if (_editMode)
                    cmd.Parameters.AddWithValue("@CompraId", _compraId!.Value);

                cmd.Parameters.AddWithValue("@UsuarioId", (Application.Current as App)?.CurrentUser?.Id ?? 1);
                cmd.Parameters.AddWithValue("@ProveedorId", ProveedorComboBox.SelectedValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Productos", json);

                var pResultado = cmd.Parameters.Add("@Resultado", SqlDbType.Bit);
                pResultado.Direction = ParameterDirection.Output;
                var pMensaje = cmd.Parameters.Add("@Mensaje", SqlDbType.NVarChar, 500);
                pMensaje.Direction = ParameterDirection.Output;

                if (!_editMode)
                {
                    cmd.Parameters.Add("@CompraId", SqlDbType.Int).Direction = ParameterDirection.Output;
                }

                await cmd.ExecuteNonQueryAsync();

                bool ok = (bool)pResultado.Value;
                string mensaje = pMensaje.Value?.ToString() ?? "Datos insertados correctamente";

                MostrarInfo(mensaje, ok ? InfoBarSeverity.Success : InfoBarSeverity.Error);

                if (ok)
                {
                    await Task.Delay(800);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MostrarInfo("Error: " + ex.Message, InfoBarSeverity.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => this.Close();

        private void MostrarInfo(string texto, InfoBarSeverity tipo)
        {
            InfoBar.Message = texto;
            InfoBar.Severity = tipo;
            InfoBar.IsOpen = true;
        }
    }
}