using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.Data.SqlClient;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ark_app1
{
    public sealed partial class InventoryPage : Page
    {
        private readonly ObservableCollection<Producto> _productos = new();
        private readonly ObservableCollection<Compra> _compras = new();
        public class IdNombre
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = "";
            public override string ToString() => Nombre;
        }
        public ObservableCollection<IdNombre> Categorias { get; } = new();
        public ObservableCollection<IdNombre> Proveedores { get; } = new();

        public InventoryPage()
        {
            this.InitializeComponent();
            Loaded += async (s, e) => await LoadInitialData();
            ProductsDataGrid.ItemsSource = _productos;
            ComprasDataGrid.ItemsSource = _compras;
        }

        private async Task LoadInitialData()
        {
            await LoadCategoriasAndProveedores();
            LoadProductos();
            LoadCompras();
        }

        private async Task LoadCategoriasAndProveedores()
        {
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Nombre FROM Categorias ORDER BY Nombre";
                    using var r1 = await cmd.ExecuteReaderAsync();
                    while (await r1.ReadAsync())
                        Categorias.Add(new IdNombre { Id = r1.GetInt32(0), Nombre = r1.GetString(1) });
                }
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Nombre FROM Proveedores ORDER BY Nombre";
                    using var r2 = await cmd.ExecuteReaderAsync();
                    while (await r2.ReadAsync())
                        Proveedores.Add(new IdNombre { Id = r2.GetInt32(0), Nombre = r2.GetString(1) });
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        private void LoadProductos(string filter = null)
        {
            _productos.Clear();
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
                    _productos.Add(new Producto
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
                    });
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        private void LoadCompras()
        {
            _compras.Clear();
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
                ShowInfoBar("Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        private async void RegistrarCompraButton_Click(object sender, RoutedEventArgs e)
        {
            var content = new AddProductDialogContent();
            content.SetCategorias(Categorias);
            content.SetProveedores(Proveedores);
            content.ConfigureForCreation();

            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "Registrar Nueva Compra",
                PrimaryButtonText = "Registrar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                Content = content,
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            if (content.productos.Count == 0)
            {
                ShowInfoBar("Error", "Agrega al menos un producto", InfoBarSeverity.Error);
                return;
            }

            var (proveedorId, json) = content.GetCompraData();
            await EjecutarRegistroCompra("sp_RegistrarCompra", proveedorId, json);
        }

        private async void EditCompraButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var compra = button.Tag as Compra;
            if (compra == null) return;

            var content = new AddProductDialogContent();
            content.SetCategorias(Categorias);
            content.SetProveedores(Proveedores);
            content.ConfigureForEdition();
            await content.LoadCompraData(compra.Id);

            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "Editar Compra ID " + compra.Id,
                PrimaryButtonText = "Guardar Cambios",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                Content = content,
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            if (content.productos.Count == 0)
            {
                ShowInfoBar("Error", "Agrega al menos un producto", InfoBarSeverity.Error);
                return;
            }

            var (proveedorId, json) = content.GetCompraData();
            await EjecutarRegistroCompra("sp_EditarCompra", proveedorId, json, compra.Id);
        }

        private async void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var p = button.Tag as Producto;
            if (p == null) return;

            var content = new AddProductDialogContent();
            content.SetCategorias(Categorias);
            content.ConfigureForProductEdition(p);

            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "Editar Producto",
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                Content = content,
                Width = 800
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var updated = content.GetProductData();
                await UpdateSingleProduct(updated);
            }
        }

        private async Task EjecutarRegistroCompra(string spName, int? proveedorId, string json, int? compraId = null)
        {
            var userId = (Application.Current as App)?.CurrentUser?.Id ?? 0;
            if (userId <= 0)
            {
                ShowInfoBar("Error", "Usuario no autenticado", InfoBarSeverity.Error);
                return;
            }

            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = spName;
                cmd.CommandType = CommandType.StoredProcedure;
                if (compraId.HasValue) cmd.Parameters.AddWithValue("@CompraId", compraId.Value);
                cmd.Parameters.AddWithValue("@ProveedorId", proveedorId.HasValue ? (object)proveedorId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@Productos", json);
                cmd.Parameters.AddWithValue("@UsuarioId", userId);
                var res = cmd.Parameters.Add("@Resultado", SqlDbType.Bit);
                var msg = cmd.Parameters.Add("@Mensaje", SqlDbType.NVarChar, 500);
                var id = cmd.Parameters.Add("@CompraId", SqlDbType.Int);
                res.Direction = msg.Direction = id.Direction = ParameterDirection.Output;

                await cmd.ExecuteNonQueryAsync();

                bool ok = (bool)res.Value;
                string mensaje = msg.Value?.ToString() ?? "";
                ShowInfoBar(ok ? "Éxito" : "Error", mensaje, ok ? InfoBarSeverity.Success : InfoBarSeverity.Error);
                if (ok)
                {
                    LoadProductos();
                    LoadCompras();
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        private async Task UpdateSingleProduct(Producto p)
        {
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Productos SET Codigo=@c, Nombre=@n, CategoriaId=@cat, Talla=@t, Color=@col, PrecioCompra=@pc, PrecioVenta=@pv, UnidadMedida=@um, StockMinimo=@sm WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", p.Id);
                cmd.Parameters.AddWithValue("@c", p.Codigo);
                cmd.Parameters.AddWithValue("@n", p.Nombre);
                cmd.Parameters.AddWithValue("@cat", p.CategoriaId.HasValue ? (object)p.CategoriaId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@t", string.IsNullOrEmpty(p.Talla) ? DBNull.Value : p.Talla);
                cmd.Parameters.AddWithValue("@col", string.IsNullOrEmpty(p.Color) ? DBNull.Value : p.Color);
                cmd.Parameters.AddWithValue("@pc", p.PrecioCompra);
                cmd.Parameters.AddWithValue("@pv", p.PrecioVenta);
                cmd.Parameters.AddWithValue("@um", p.UnidadMedida ?? "Unidad");
                cmd.Parameters.AddWithValue("@sm", p.StockMinimo);
                await cmd.ExecuteNonQueryAsync();
                LoadProductos();
                ShowInfoBar("Éxito", "Producto actualizado", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowInfoBar("Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadProductos(SearchTextBox.Text);
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