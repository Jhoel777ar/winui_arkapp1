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
        private Producto _originalProduct;

        public InventoryPage()
        {
            this.InitializeComponent();
            LoadCategorias();
            LoadProductos();
            ProductsDataGrid.ItemsSource = _productos;
        }

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

        private async void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            var content = new AddProductDialogContent();
            content.SetCategorias(Categorias);

            var dialog = new ContentDialog();
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = "Agregar Nuevo Producto";
            dialog.PrimaryButtonText = "Guardar";
            dialog.CloseButtonText = "Cancelar";
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = content;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var newProduct = content.GetProduct();
                SaveNewProduct(newProduct);
            }
        }

        private void SaveNewProduct(Producto product)
        {
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Productos (Codigo, Nombre, CategoriaId, Talla, Color, PrecioCompra, PrecioVenta, Stock, UnidadMedida, StockMinimo) VALUES (@codigo, @nombre, @categoriaId, @talla, @color, @precioCompra, @precioVenta, @stock, @unidadMedida, @stockMinimo)";
                
                cmd.Parameters.AddWithValue("@codigo", product.Codigo);
                cmd.Parameters.AddWithValue("@nombre", product.Nombre);
                cmd.Parameters.AddWithValue("@categoriaId", product.CategoriaId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@talla", string.IsNullOrEmpty(product.Talla) ? DBNull.Value : product.Talla);
                cmd.Parameters.AddWithValue("@color", string.IsNullOrEmpty(product.Color) ? DBNull.Value : product.Color);
                cmd.Parameters.AddWithValue("@precioCompra", product.PrecioCompra);
                cmd.Parameters.AddWithValue("@precioVenta", product.PrecioVenta);
                cmd.Parameters.AddWithValue("@stock", product.Stock);
                cmd.Parameters.AddWithValue("@unidadMedida", product.UnidadMedida);
                cmd.Parameters.AddWithValue("@stockMinimo", product.StockMinimo);

                cmd.ExecuteNonQuery();
                LoadProductos(SearchTextBox.Text);
                ShowInfoBar("Éxito", "Producto guardado correctamente.", InfoBarSeverity.Success);

            }
            catch (Exception ex)
            {
                ShowInfoBar("Error", $"Error al guardar producto: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void ProductsDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            _originalProduct = e.Row.DataContext as Producto;
        }

        private void ProductsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var editedProduct = e.Row.DataContext as Producto;
                if (!editedProduct.Equals(_originalProduct))
                {
                    UpdateProduct(editedProduct);
                }
            }
            else if (e.EditAction == DataGridEditAction.Cancel)
            {
                // Restaurar el producto original si se cancela la edición
                var productInView = e.Row.DataContext as Producto;
                productInView.CopyFrom(_originalProduct);
            }
        }

        private void UpdateProduct(Producto product)
        {
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Productos SET Nombre = @nombre, CategoriaId = @categoriaId, PrecioVenta = @precioVenta, Stock = @stock WHERE Id = @id";
                
                cmd.Parameters.AddWithValue("@id", product.Id);
                cmd.Parameters.AddWithValue("@nombre", product.Nombre);
                cmd.Parameters.AddWithValue("@categoriaId", product.CategoriaId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@precioVenta", product.PrecioVenta);
                cmd.Parameters.AddWithValue("@stock", product.Stock);

                cmd.ExecuteNonQuery();
                ShowInfoBar("Éxito", "Producto actualizado correctamente.", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowInfoBar("Error", $"Error al actualizar producto: {ex.Message}", InfoBarSeverity.Error);
                 LoadProductos(SearchTextBox.Text); // Recargar para deshacer cambios fallidos
            }
        }
        
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var productId = (int)button.Tag;
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Productos WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", productId);
                cmd.ExecuteNonQuery();
                LoadProductos(SearchTextBox.Text);
                ShowInfoBar("Éxito", "Producto eliminado correctamente.", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowInfoBar("Error", $"Error al eliminar producto: {ex.Message}", InfoBarSeverity.Error);
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
            Info.IsOpen = true;
            Info.IsClosable = true;
        }
    }
}
