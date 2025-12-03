using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.Data.SqlClient;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ark_app1
{
    public sealed partial class InventoryPage : Page
    {
        private readonly ObservableCollection<Producto> _productos = new ObservableCollection<Producto>();
        public List<Tuple<int, string>> Categorias { get; set; }

        public InventoryPage()
        {
            this.InitializeComponent();
            LoadCategorias();
            LoadProductos();
            ProductsDataGrid.ItemsSource = _productos;
        }

        // --- Métodos de Carga de Datos ---

        private void LoadCategorias()
        {
            Categorias = new List<Tuple<int, string>>();
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, Nombre FROM Categorias";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Categorias.Add(new Tuple<int, string>(reader.GetInt32(0), reader.GetString(1)));
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("Error", $"Error al cargar categorías: {ex.Message}", InfoBarSeverity.Error);
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
                                    FROM Productos p 
                                    LEFT JOIN Categorias c ON p.CategoriaId = c.Id";

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    cmd.CommandText += " WHERE p.Nombre LIKE @filter OR p.Codigo LIKE @filter";
                    cmd.Parameters.AddWithValue("@filter", $"%{filter}%");
                }

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    _productos.Add(new Producto
                    {
                        Id = reader.GetInt32(0),
                        Codigo = reader.GetString(1),
                        Nombre = reader.GetString(2),
                        CategoriaId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                        CategoriaNombre = reader.IsDBNull(4) ? "Sin Categoría" : reader.GetString(4),
                        Talla = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Color = reader.IsDBNull(6) ? null : reader.GetString(6),
                        PrecioCompra = reader.GetDecimal(7),
                        PrecioVenta = reader.GetDecimal(8),
                        Stock = reader.GetDecimal(9),
                        UnidadMedida = reader.GetString(10),
                        StockMinimo = reader.GetDecimal(11),
                        FechaRegistro = reader.GetDateTime(12)
                    });
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("Error", $"Error al cargar productos: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        // --- Eventos de Botones ---

        private async void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            await OpenProductDialog(null); // null indica que es un producto nuevo
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var productToEdit = button.Tag as Producto;
            if (productToEdit != null)
            {
                await OpenProductDialog(productToEdit); 
            }
        }

        private async System.Threading.Tasks.Task OpenProductDialog(Producto product)
        {
            var content = new AddProductDialogContent();
            content.SetCategorias(Categorias);

            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                Content = content
            };

            if (product == null)
            {
                dialog.Title = "Agregar Nuevo Producto";
                content.ClearForm();
            }
            else
            {
                dialog.Title = "Editar Producto";
                content.LoadProduct(product);
            }

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var productFromDialog = content.GetProduct();
                if (product == null)
                {
                    SaveNewProduct(productFromDialog);
                }
                else
                {
                    UpdateProduct(productFromDialog);
                }
            }
        }

        // --- Métodos de la Base de Datos ---

        private void SaveNewProduct(Producto product)
        {
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Productos (Codigo, Nombre, CategoriaId, Talla, Color, PrecioCompra, PrecioVenta, Stock, UnidadMedida, StockMinimo) VALUES (@codigo, @nombre, @categoriaId, @talla, @color, @precioCompra, @precioVenta, @stock, @unidadMedida, @stockMinimo)";
                AddProductParameters(cmd, product);
                cmd.ExecuteNonQuery();
                LoadProductos(SearchTextBox.Text);
                ShowInfoBar("Éxito", "Producto guardado correctamente.", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowInfoBar("Error", $"Error al guardar producto: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void UpdateProduct(Producto product)
        {
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Productos SET Codigo = @codigo, Nombre = @nombre, CategoriaId = @categoriaId, Talla = @talla, Color = @color, PrecioCompra = @precioCompra, PrecioVenta = @precioVenta, Stock = @stock, UnidadMedida = @unidadMedida, StockMinimo = @stockMinimo WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", product.Id);
                AddProductParameters(cmd, product);
                cmd.ExecuteNonQuery();
                LoadProductos(SearchTextBox.Text);
                ShowInfoBar("Éxito", "Producto actualizado correctamente.", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowInfoBar("Error", $"Error al actualizar producto: {ex.Message}", InfoBarSeverity.Error);
            }
        }
        
        private void AddProductParameters(SqlCommand cmd, Producto product)
        {
            cmd.Parameters.AddWithValue("@codigo", product.Codigo);
            cmd.Parameters.AddWithValue("@nombre", product.Nombre);
            cmd.Parameters.AddWithValue("@categoriaId", product.CategoriaId.HasValue ? (object)product.CategoriaId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@talla", string.IsNullOrEmpty(product.Talla) ? DBNull.Value : product.Talla);
            cmd.Parameters.AddWithValue("@color", string.IsNullOrEmpty(product.Color) ? DBNull.Value : product.Color);
            cmd.Parameters.AddWithValue("@precioCompra", product.PrecioCompra);
            cmd.Parameters.AddWithValue("@precioVenta", product.PrecioVenta);
            cmd.Parameters.AddWithValue("@stock", product.Stock);
            cmd.Parameters.AddWithValue("@unidadMedida", product.UnidadMedida);
            cmd.Parameters.AddWithValue("@stockMinimo", product.StockMinimo);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var productId = (int)button.Tag;
            // Lógica de eliminación (sin cambios)
        }

        // --- UI y Búsqueda ---

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
            InfoBar.IsClosable = true;
        }
    }
}
