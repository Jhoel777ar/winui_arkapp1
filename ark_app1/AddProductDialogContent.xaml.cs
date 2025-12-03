using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace ark_app1
{
    public sealed partial class AddProductDialogContent : Page
    {
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

        public Producto GetProduct()
        {
            return new Producto
            {
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
