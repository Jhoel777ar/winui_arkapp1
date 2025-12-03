using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ark_app1
{
    public sealed partial class InventoryPage : Page
    {
        private readonly ObservableCollection<Producto> _productos = new();
        private readonly ObservableCollection<Compra> _compras = new();

        public InventoryPage()
        {
            this.InitializeComponent();
            Loaded += async (s, e) => await LoadInitialData();
            ProductsDataGrid.ItemsSource = _productos;
            ComprasDataGrid.ItemsSource = _compras;
        }

        private async Task LoadInitialData()
        {
            await LoadProductos();
            await LoadCompras();
        }

        private async Task LoadProductos(string filter = null)
        {
            _productos.Clear();
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT p.Id, p.Codigo, p.Nombre, p.CategoriaId, c.Nombre as CategoriaNombre, p.Talla, p.Color, 
                                          p.PrecioCompra, p.PrecioVenta, p.Stock, p.UnidadMedida, p.StockMinimo, p.FechaRegistro
                                   FROM Productos p LEFT JOIN Categorias c ON p.CategoriaId = c.Id";

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    cmd.CommandText += " WHERE p.Nombre LIKE @f OR p.Codigo LIKE @f";
                    cmd.Parameters.AddWithValue("@f", $"%{filter}%");
                }
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    _productos.Add(new Producto
                    {
                        Id = r.GetInt32(0),
                        Codigo = r.GetString(1),
                        Nombre = r.GetString(2),
                        CategoriaId = r.IsDBNull(3) ? null : r.GetInt32(3),
                        CategoriaNombre = r.IsDBNull(4) ? "Sin Categoría" : r.GetString(4),
                        Talla = r.IsDBNull(5) ? "" : r.GetString(5),
                        Color = r.IsDBNull(6) ? "" : r.GetString(6),
                        PrecioCompra = r.GetDecimal(7),
                        PrecioVenta = r.GetDecimal(8),
                        Stock = r.GetDecimal(9),
                        UnidadMedida = r.GetString(10),
                        StockMinimo = r.GetDecimal(11),
                        FechaRegistro = r.GetDateTime(12)
                    });
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("Error al cargar productos", ex.Message, InfoBarSeverity.Error);
            }
        }

        private async Task LoadCompras()
        {
            _compras.Clear();
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"SELECT c.Id, c.Fecha, p.Nombre as Proveedor, c.Total, u.NombreCompleto as Usuario, c.Estado
                                           FROM Compras c LEFT JOIN Proveedores p ON c.ProveedorId = p.Id
                                           LEFT JOIN Usuarios u ON c.UsuarioId = u.Id ORDER BY c.Fecha DESC", conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    _compras.Add(new Compra
                    {
                        Id = r.GetInt32(0),
                        Fecha = r.GetDateTime(1),
                        Proveedor = r.IsDBNull(2) ? "Sin Proveedor" : r.GetString(2),
                        Total = r.GetDecimal(3),
                        Usuario = r.GetString(4),
                        Estado = r.GetString(5)
                    });
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("Error al cargar compras", ex.Message, InfoBarSeverity.Error);
            }
        }

        private void RegistrarCompraButton_Click(object sender, RoutedEventArgs e)
        {
            var addCompraWindow = new AddProductDialogContent();
            addCompraWindow.CompraSaved += OnCompraSaved;
            addCompraWindow.Activate();
        }

        private void EditCompraButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: Compra compra }) return;

            var editCompraWindow = new AddProductDialogContent(compra.Id);
            editCompraWindow.CompraSaved += OnCompraSaved;
            editCompraWindow.Activate();
        }

        private void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para editar el producto aquí
        }

        private async void OnCompraSaved(object sender, EventArgs e)
        {
            if(sender is Window w) w.Close();
            ShowInfoBar("Operación exitosa", "Los datos se han guardado.", InfoBarSeverity.Success);
            await LoadInitialData();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ = LoadProductos(SearchTextBox.Text);
        }

        private void ShowInfoBar(string title, string message, InfoBarSeverity severity)
        {
            InfoBar.Title = title;
            InfoBar.Message = message;
            InfoBar.Severity = severity;
            InfoBar.IsOpen = true;
        }
    }

    public class Compra
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Proveedor { get; set; }
        public decimal Total { get; set; }
        public string Usuario { get; set; }
        public string Estado { get; set; }
    }
}
