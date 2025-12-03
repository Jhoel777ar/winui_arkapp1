using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace ark_app1
{
    public sealed partial class AddProductDialogContent : Page
    {
        private int? _productId;

        public AddProductDialogContent()
        {
            this.InitializeComponent();
        }

        public void SetCategorias(object itemsSource)
        {
            CategoriaComboBox.ItemsSource = itemsSource;
            CategoriaComboBox.DisplayMemberPath = "Item2";
            CategoriaComboBox.SelectedValuePath = "Item1";
        }

        // Carga los datos de un producto existente en el formulario para editarlo
        public void LoadProduct(Producto product)
        {
            _productId = product.Id;
            CodigoTextBox.Text = product.Codigo;
            NombreTextBox.Text = product.Nombre;
            CategoriaComboBox.SelectedValue = product.CategoriaId;
            TallaTextBox.Text = product.Talla;
            ColorTextBox.Text = product.Color;
            PrecioCompraNumberBox.Value = (double)product.PrecioCompra;
            PrecioVentaNumberBox.Value = (double)product.PrecioVenta;
            StockNumberBox.Value = (double)product.Stock;
            UnidadMedidaTextBox.Text = product.UnidadMedida;
            StockMinimoNumberBox.Value = (double)product.StockMinimo;
        }

        // Limpia el formulario para crear un nuevo producto
        public void ClearForm()
        {
            _productId = null;
            CodigoTextBox.Text = "";
            NombreTextBox.Text = "";
            CategoriaComboBox.SelectedIndex = -1;
            TallaTextBox.Text = "";
            ColorTextBox.Text = "";
            PrecioCompraNumberBox.Value = 0;
            PrecioVentaNumberBox.Value = 0;
            StockNumberBox.Value = 0;
            UnidadMedidaTextBox.Text = "";
            StockMinimoNumberBox.Value = 0;
        }

        public Producto GetProduct()
        {
            return new Producto
            {
                Id = _productId ?? 0, // Si es un producto nuevo, Id será 0
                Codigo = CodigoTextBox.Text,
                Nombre = NombreTextBox.Text,
                CategoriaId = (int?)CategoriaComboBox.SelectedValue,
                Talla = TallaTextBox.Text,
                Color = ColorTextBox.Text,
                PrecioCompra = (decimal)PrecioCompraNumberBox.Value,
                PrecioVenta = (decimal)PrecioVentaNumberBox.Value,
                Stock = (decimal)StockNumberBox.Value,
                UnidadMedida = UnidadMedidaTextBox.Text,
                StockMinimo = (decimal)StockMinimoNumberBox.Value
            };
        }
    }
}
