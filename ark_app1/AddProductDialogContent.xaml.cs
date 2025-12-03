using Microsoft.Data.SqlClient;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace ark_app1
{
    public class ItemCompra
    {
        public int? ProductoId { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public int? CategoriaId { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal Cantidad { get; set; } = 1;
        public string UnidadMedida { get; set; } = "Unidad";
        public decimal StockMinimo { get; set; } = 5;
        public string DisplayText => ProductoId.HasValue ? $"ID {ProductoId} - {Nombre} x{Cantidad} (P.Venta: {PrecioVenta})" : $"{Codigo} - {Nombre} x{Cantidad} (P.Venta: {PrecioVenta})";
    }

    public sealed partial class AddProductDialogContent : Page
    {
        public readonly ObservableCollection<ItemCompra> productos = new();
        private bool isEditionMode = false;

        public AddProductDialogContent()
        {
            this.InitializeComponent();
            ProductosItemsControl.ItemsSource = productos;
        }

        public void SetCategorias(object itemsSource)
        {
            CategoriaComboBox.ItemsSource = itemsSource;
            CategoriaComboBox.DisplayMemberPath = "Nombre";
            CategoriaComboBox.SelectedValuePath = "Id";
        }

        public void SetProveedores(object itemsSource)
        {
            ProveedorComboBox.ItemsSource = itemsSource;
            ProveedorComboBox.DisplayMemberPath = "Nombre";
            ProveedorComboBox.SelectedValuePath = "Id";
        }

        public void ConfigureForCreation()
        {
            isEditionMode = false;
            CodigoTextBox.Visibility = Visibility.Visible;
            ClearForm();
        }

        public void ConfigureForEdition()
        {
            isEditionMode = true;
            CodigoTextBox.Visibility = Visibility.Collapsed;
            ClearForm();
        }

        public void ConfigureForProductEdition(Producto p)
        {
            isEditionMode = true;
            ProveedorComboBox.Visibility = Visibility.Collapsed;
            CantidadNumberBox.Visibility = Visibility.Collapsed;
            ProductosItemsControl.Visibility = Visibility.Collapsed;
            CodigoTextBox.Text = p.Codigo;
            NombreTextBox.Text = p.Nombre;
            CategoriaComboBox.SelectedValue = p.CategoriaId;
            TallaTextBox.Text = p.Talla ?? "";
            ColorTextBox.Text = p.Color ?? "";
            PrecioCompraNumberBox.Value = (double)p.PrecioCompra;
            PrecioVentaNumberBox.Value = (double)p.PrecioVenta;
            UnidadMedidaTextBox.Text = p.UnidadMedida ?? "Unidad";
            StockMinimoNumberBox.Value = (double)p.StockMinimo;
        }

        public async Task LoadCompraData(int compraId)
        {
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT ProveedorId FROM Compras WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", compraId);
                var provId = await cmd.ExecuteScalarAsync() as int?;
                ProveedorComboBox.SelectedValue = provId;

                cmd.CommandText = @"SELECT p.Id AS ProductoId, p.Nombre, p.CategoriaId, p.Talla, p.Color, cd.PrecioUnitario AS PrecioCompra, p.PrecioVenta, cd.Cantidad, p.UnidadMedida, p.StockMinimo
                                    FROM ComprasDetalle cd INNER JOIN Productos p ON cd.ProductoId = p.Id WHERE cd.CompraId = @id";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@id", compraId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    productos.Add(new ItemCompra
                    {
                        ProductoId = r.GetInt32(0),
                        Nombre = r.GetString(1),
                        CategoriaId = r.IsDBNull(2) ? null : r.GetInt32(2),
                        Talla = r.IsDBNull(3) ? "" : r.GetString(3),
                        Color = r.IsDBNull(4) ? "" : r.GetString(4),
                        PrecioCompra = r.GetDecimal(5),
                        PrecioVenta = r.GetDecimal(6),
                        Cantidad = r.GetDecimal(7),
                        UnidadMedida = r.GetString(8),
                        StockMinimo = r.GetDecimal(9)
                    });
                }
            }
            catch (Exception) { }
        }

        public void ClearForm()
        {
            CodigoTextBox.Text = "";
            NombreTextBox.Text = "";
            CategoriaComboBox.SelectedIndex = -1;
            TallaTextBox.Text = "";
            ColorTextBox.Text = "";
            PrecioCompraNumberBox.Value = 0;
            PrecioVentaNumberBox.Value = 0;
            CantidadNumberBox.Value = 1;
            UnidadMedidaTextBox.Text = "Unidad";
            StockMinimoNumberBox.Value = 5;
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(isEditionMode ? "" : CodigoTextBox.Text) ||
                string.IsNullOrWhiteSpace(NombreTextBox.Text) ||
                PrecioVentaNumberBox.Value <= 0 ||
                CantidadNumberBox.Value <= 0)
                return;

            productos.Add(new ItemCompra
            {
                ProductoId = isEditionMode ? (int?)0 : null,
                Codigo = isEditionMode ? "" : CodigoTextBox.Text.Trim(),
                Nombre = NombreTextBox.Text.Trim(),
                CategoriaId = CategoriaComboBox.SelectedValue as int?,
                Talla = TallaTextBox.Text.Trim(),
                Color = ColorTextBox.Text.Trim(),
                PrecioCompra = (decimal)PrecioCompraNumberBox.Value,
                PrecioVenta = (decimal)PrecioVentaNumberBox.Value,
                Cantidad = (decimal)CantidadNumberBox.Value,
                UnidadMedida = UnidadMedidaTextBox.Text.Trim(),
                StockMinimo = (decimal)StockMinimoNumberBox.Value
            });

            ClearForm();
        }

        private void RemoveProduct_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ItemCompra item)
                productos.Remove(item);
        }

        public (int? proveedorId, string json) GetCompraData()
        {
            var proveedorId = ProveedorComboBox.SelectedValue as int?;

            var json = JsonConvert.SerializeObject(productos.Select(p => new
            {
                Codigo = p.Codigo,
                p.Nombre,
                p.CategoriaId,
                Talla = p.Talla ?? (string)null,
                Color = p.Color ?? (string)null,
                p.PrecioCompra,
                p.PrecioVenta,
                p.Cantidad,
                p.UnidadMedida,
                p.StockMinimo
            }));

            return (proveedorId, json);
        }

        public Producto GetProductData()
        {
            return new Producto
            {
                Codigo = CodigoTextBox.Text,
                Nombre = NombreTextBox.Text,
                CategoriaId = CategoriaComboBox.SelectedValue as int?,
                Talla = TallaTextBox.Text,
                Color = ColorTextBox.Text,
                PrecioCompra = (decimal)PrecioCompraNumberBox.Value,
                PrecioVenta = (decimal)PrecioVentaNumberBox.Value,
                UnidadMedida = UnidadMedidaTextBox.Text,
                StockMinimo = (decimal)StockMinimoNumberBox.Value
            };
        }
    }
}