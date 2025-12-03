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
                var productList = await DatabaseManager.Instance.GetProductsAsync(filter);
                foreach (var p in productList)
                {
                    _productos.Add(p);
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
            addCompraWindow.Activate(); // Muestra la ventana
        }

        private void EditCompraButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: Compra compra }) return;

            var editCompraWindow = new AddProductDialogContent(compra.Id);
            editCompraWindow.CompraSaved += OnCompraSaved;
            editCompraWindow.Activate(); // Muestra la ventana
        }

        private async void OnCompraSaved(object sender, EventArgs e)
        {
            (sender as Window)?.Close(); // Cierra la ventana de compra
            ShowInfoBar("Operación exitosa", "Los datos se han guardado correctamente.", InfoBarSeverity.Success);
            await LoadInitialData(); // Recarga los datos
        }

        private async void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            ShowInfoBar("Función no disponible", "La edición de un producto individual aún no está implementada con el nuevo diseño.", InfoBarSeverity.Informational);
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
}
