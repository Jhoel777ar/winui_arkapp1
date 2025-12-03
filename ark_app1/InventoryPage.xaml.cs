using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.Data.SqlClient;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
            // Cargar datos de forma asÃ­ncrona y en paralelo
            await Task.WhenAll(LoadProductos(), LoadCompras());
        }

        private async Task LoadProductos(string filter = null)
        {
            await Task.Run(() =>
            {
                DispatcherQueue.TryEnqueue(() => _productos.Clear());

                try
                {
                    using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = @"SELECT p.Id, p.Codigo, p.Nombre, p.CategoriaId, c.Nombre as CategoriaNombre, p.Talla, p.Color, p.PrecioCompra, p.PrecioVenta, p.Stock, p.UnidadMedida, p.StockMinimo, p.FechaRegistro
                                        FROM Productos p LEFT JOIN Categorias c ON p.CategoriaId = c.Id";
                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        cmd.CommandText += " WHERE p.Nombre LIKE @f OR p.Codigo LIKE @f";
                        cmd.Parameters.AddWithValue("@f", $"%{filter}%");
                    }
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        var producto = new Producto
                        {
                            Id = r.GetInt32(0),
                            Codigo = r.GetString(1),
                            Nombre = r.GetString(2),
                            CategoriaId = r.IsDBNull(3) ? null : r.GetInt32(3),
                            CategoriaNombre = r.IsDBNull(4) ? "Sin Categoría" : r.GetString(4),
                            Talla = r.IsDBNull(5) ? null : r.GetString(5),
                            Color = r.IsDBNull(6) ? null : r.GetString(6),
                            PrecioCompra = r.GetDecimal(7),
                            PrecioVenta = r.GetDecimal(8),
                            Stock = r.GetDecimal(9),
                            UnidadMedida = r.GetString(10),
                            StockMinimo = r.GetDecimal(11),
                            FechaRegistro = r.GetDateTime(12)
                        };
                        DispatcherQueue.TryEnqueue(() => _productos.Add(producto));
                    }
                }
                catch (Exception ex)
                {
                    DispatcherQueue.TryEnqueue(() => ShowInfoBar("Error al cargar productos", ex.Message, InfoBarSeverity.Error));
                }
            });
        }

        private async Task LoadCompras()
        {
            await Task.Run(() =>
            {
                DispatcherQueue.TryEnqueue(() => _compras.Clear());
                try
                {
                    using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = @"SELECT c.Id, c.Fecha, p.Nombre as Proveedor, c.Total, u.NombreCompleto as Usuario, c.Estado
                                        FROM Compras c LEFT JOIN Proveedores p ON c.ProveedorId = p.Id
                                        LEFT JOIN Usuarios u ON c.UsuarioId = u.Id ORDER BY c.Fecha DESC";
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        var compra = new Compra
                        {
                            Id = r.GetInt32(0),
                            Fecha = r.GetDateTime(1),
                            Proveedor = r.IsDBNull(2) ? "Sin Proveedor" : r.GetString(2),
                            Total = r.GetDecimal(3),
                            Usuario = r.GetString(4),
                            Estado = r.GetString(5)
                        };
                         DispatcherQueue.TryEnqueue(() => _compras.Add(compra));
                    }
                }
                catch (Exception ex)
                {
                    DispatcherQueue.TryEnqueue(() => ShowInfoBar("Error al cargar compras", ex.Message, InfoBarSeverity.Error));
                }
            });
        }

        private void RegistrarCompraButton_Click(object sender, RoutedEventArgs e)
        {
            var addCompraWindow = new AddProductDialogContent();
            addCompraWindow.CompraSaved += OnCompraSaved; // Suscribirse al evento
            addCompraWindow.Activate();
        }

        private void EditCompraButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: Compra compra } ) return;

            var editCompraWindow = new AddProductDialogContent(compra.Id);
            editCompraWindow.CompraSaved += OnCompraSaved; // Suscribirse al evento
            editCompraWindow.Activate();
        }

        private async void OnCompraSaved(object sender, EventArgs e)
        {
            ShowInfoBar("Operación exitosa", "Los datos se han guardado correctamente.", InfoBarSeverity.Success);
            // Recargar los datos para reflejar los cambios
            await LoadInitialData();
        }


        private async void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: La ediciÃ³n de productos individuales ahora requiere su propia ventana/diÃ¡logo.
            //La ventana AddProductDialogContent fue refactorizada para manejar Ãºnicamente la creaciÃ³n y ediciÃ³n de COMPRAS (listas de productos).
            //Se necesita crear una nueva interfaz para editar un solo producto.
            ShowInfoBar("FunciÃ³n no disponible", "La ediciÃ³n de un producto individual aÃºn no estÃ¡ implementada con el nuevo diseÃ±o.", InfoBarSeverity.Informational);
            await Task.CompletedTask;
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
