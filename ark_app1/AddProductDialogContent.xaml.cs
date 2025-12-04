using Microsoft.UI.Xaml.Controls;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;

namespace ark_app1
{
    public sealed partial class AddProductDialogContent : Page
    {
        public AddProductDialogContent()
        {
            this.InitializeComponent();
            _ = LoadCategorias();
        }

        private async Task LoadCategorias()
        {
            try
            {
                var cats = new ObservableCollection<object>();
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT Id, Nombre FROM Categorias ORDER BY Nombre", conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    cats.Add(new { Id = r.GetInt32(0), Nombre = r.GetString(1) });
                }
                CategoriaComboBox.ItemsSource = cats;
            }
            catch { /* Ignore for now */ }
        }

        public Producto GetProducto()
        {
            if (string.IsNullOrWhiteSpace(CodigoTextBox.Text) || string.IsNullOrWhiteSpace(NombreTextBox.Text))
            {
                ErrorTextBlock.Text = "Código y Nombre son obligatorios.";
                ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                return null;
            }

            return new Producto
            {
                Codigo = CodigoTextBox.Text,
                Nombre = NombreTextBox.Text,
                CategoriaId = (int?)CategoriaComboBox.SelectedValue,
                Talla = TallaTextBox.Text,
                Color = ColorTextBox.Text,
                PrecioCompra = (decimal)PrecioCompraBox.Value,
                PrecioVenta = (decimal)PrecioVentaBox.Value,
                Stock = (decimal)CantidadBox.Value, // Temporarily holding quantity here
                UnidadMedida = UnidadTextBox.Text,
                StockMinimo = (decimal)StockMinimoBox.Value
            };
        }
    }
}
