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
        private ObservableCollection<Producto> _productos = new ObservableCollection<Producto>();

        public InventoryPage()
        {
            this.InitializeComponent();
            LoadCategorias();
            LoadProductos();
            ProductsListView.ItemsSource = _productos;
        }

        private void LoadCategorias()
        {
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, Nombre FROM Categorias";
                using var reader = cmd.ExecuteReader();
                var categorias = new List<Tuple<int, string>>();
                while (reader.Read())
                {
                    categorias.Add(new Tuple<int, string>(reader.GetInt32(0), reader.GetString(1)));
                }
                CategoriaComboBox.ItemsSource = categorias;
                CategoriaComboBox.DisplayMemberPath = "Item2";
                CategoriaComboBox.SelectedValuePath = "Item1";
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

        private void SaveProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                conn.Open();
                var cmd = conn.CreateCommand();
                
                // Asumiendo que un producto nuevo no tiene Id o su Id es 0
                bool isNewProduct = ProductsListView.SelectedItem == null || ((Producto)ProductsListView.SelectedItem).Id == 0;

                if (isNewProduct)
                {
                    cmd.CommandText = "INSERT INTO Productos (Codigo, Nombre, CategoriaId, Talla, Color, PrecioCompra, PrecioVenta, Stock, UnidadMedida, StockMinimo) VALUES (@codigo, @nombre, @categoriaId, @talla, @color, @precioCompra, @precioVenta, @stock, @unidadMedida, @stockMinimo)";
                }
                else
                {
                    cmd.CommandText = "UPDATE Productos SET Codigo = @codigo, Nombre = @nombre, CategoriaId = @categoriaId, Talla = @talla, Color = @color, PrecioCompra = @precioCompra, PrecioVenta = @precioVenta, Stock = @stock, UnidadMedida = @unidadMedida, StockMinimo = @stockMinimo WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", ((Producto)ProductsListView.SelectedItem).Id);
                }

                cmd.Parameters.AddWithValue("@codigo", CodigoTextBox.Text);
                cmd.Parameters.AddWithValue("@nombre", NombreTextBox.Text);
                cmd.Parameters.AddWithValue("@categoriaId", CategoriaComboBox.SelectedValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@talla", string.IsNullOrEmpty(TallaTextBox.Text) ? DBNull.Value : TallaTextBox.Text);
                cmd.Parameters.AddWithValue("@color", string.IsNullOrEmpty(ColorTextBox.Text) ? DBNull.Value : ColorTextBox.Text);
                cmd.Parameters.AddWithValue("@precioCompra", PrecioCompraNumberBox.Value);
                cmd.Parameters.AddWithValue("@precioVenta", PrecioVentaNumberBox.Value);
                cmd.Parameters.AddWithValue("@stock", StockNumberBox.Value);
                cmd.Parameters.AddWithValue("@unidadMedida", UnidadMedidaTextBox.Text);
                cmd.Parameters.AddWithValue("@stockMinimo", StockMinimoNumberBox.Value);

                cmd.ExecuteNonQuery();
                LoadProductos();
                ClearForm();
                ShowInfoBar("Éxito", "Producto guardado correctamente.", InfoBarSeverity.Success);

            }
            catch (Exception ex)
            {
                ShowInfoBar("Error", $"Error al guardar producto: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var productId = (int)button.Tag;
            var producto = _productos.FirstOrDefault(p => p.Id == productId);
            if (producto != null)
            {
                CodigoTextBox.Text = producto.Codigo;
                NombreTextBox.Text = producto.Nombre;
                CategoriaComboBox.SelectedValue = producto.CategoriaId;
                TallaTextBox.Text = producto.Talla;
                ColorTextBox.Text = producto.Color;
                PrecioCompraNumberBox.Value = (double)producto.PrecioCompra;
                PrecioVentaNumberBox.Value = (double)producto.PrecioVenta;
                StockNumberBox.Value = (double)producto.Stock;
                UnidadMedidaTextBox.Text = producto.UnidadMedida;
                StockMinimoNumberBox.Value = (double)producto.StockMinimo;
                ProductsListView.SelectedItem = producto;
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
                LoadProductos();
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

        private void ClearForm()
        {
            CodigoTextBox.Text = string.Empty;
            NombreTextBox.Text = string.Empty;
            CategoriaComboBox.SelectedIndex = -1;
            TallaTextBox.Text = string.Empty;
            ColorTextBox.Text = string.Empty;
            PrecioCompraNumberBox.Value = 0;
            PrecioVentaNumberBox.Value = 0;
            StockNumberBox.Value = 0;
            UnidadMedidaTextBox.Text = "Unidad";
            StockMinimoNumberBox.Value = 5;
            ProductsListView.SelectedItem = null;
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
