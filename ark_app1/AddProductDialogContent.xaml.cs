
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Text.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ark_app1
{
    public sealed partial class AddProductDialogContent : Window
    {
        private ObservableCollection<Producto> _productosEnCompra;
        private bool _isEditMode = false;
        private int? _compraIdToEdit = null;

        public AddProductDialogContent()
        {
            this.InitializeComponent();
            _productosEnCompra = new ObservableCollection<Producto>();
            ProductosListView.ItemsSource = _productosEnCompra;
            LoadCategorias();
        }

        public AddProductDialogContent(int compraId) : this()
        {
            _isEditMode = true;
            _compraIdToEdit = compraId;
            Title = "Editar Compra";
            SaveCompraButton.Content = "Guardar Cambios";
            // TODO: Load existing compra data
        }

        private async void LoadCategorias()
        {
            try
            {
                var categorias = await DatabaseManager.Instance.GetCategoriasAsync();
                CategoriaComboBox.ItemsSource = categorias;
                CategoriaComboBox.DisplayMemberPath = "Nombre";
                CategoriaComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                ShowMessage("Error", "No se pudieron cargar las categorías: " + ex.Message, InfoBarSeverity.Error);
            }
        }

        private void AddProductToListButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateProductForm()) return;

            var producto = new Producto
            {
                Codigo = CodigoTextBox.Text,
                Nombre = NombreTextBox.Text,
                CategoriaId = (int?)CategoriaComboBox.SelectedValue,
                Talla = TallaTextBox.Text,
                Color = ColorTextBox.Text,
                PrecioCompra = (decimal)PrecioCompraNumberBox.Value,
                PrecioVenta = (decimal)PrecioVentaNumberBox.Value,
                Cantidad = (decimal)CantidadNumberBox.Value,
                UnidadMedida = UnidadMedidaTextBox.Text,
                StockMinimo = (decimal)StockMinimoNumberBox.Value
            };

            _productosEnCompra.Add(producto);
            ClearProductForm();
        }

        private async void SaveCompraButton_Click(object sender, RoutedEventArgs e)
        {
            if (_productosEnCompra.Count == 0)
            {
                ShowMessage("Error", "Debe agregar al menos un producto a la compra.", InfoBarSeverity.Warning);
                return;
            }

            string jsonProductos = JsonSerializer.Serialize(_productosEnCompra.Select(p => new {
                p.ProductoId, p.Codigo, p.Nombre, p.CategoriaId, p.Talla, p.Color,
                p.PrecioCompra, p.PrecioVenta, p.Cantidad, p.UnidadMedida, p.StockMinimo
            }));

            try
            {
                using (var conn = await DatabaseManager.Instance.GetOpenConnectionAsync())
                {
                    var cmd = new SqlCommand(_isEditMode ? "sp_EditarCompra" : "sp_RegistrarCompra", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    if (_isEditMode)
                    {
                        cmd.Parameters.AddWithValue("@CompraId", _compraIdToEdit.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@UsuarioId", 1); // Replace with actual user ID
                        cmd.Parameters.AddWithValue("@ProveedorId", DBNull.Value); // Add supplier selection if needed
                    }

                    cmd.Parameters.AddWithValue("@Productos", jsonProductos);

                    var resultadoParam = new SqlParameter("@Resultado", System.Data.SqlDbType.Bit) { Direction = System.Data.ParameterDirection.Output };
                    var mensajeParam = new SqlParameter("@Mensaje", System.Data.SqlDbType.NVarChar, 500) { Direction = System.Data.ParameterDirection.Output };
                    cmd.Parameters.Add(resultadoParam);
                    cmd.Parameters.Add(mensajeParam);

                    if (!_isEditMode)
                    {
                        var compraIdParam = new SqlParameter("@CompraId", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
                        cmd.Parameters.Add(compraIdParam);
                    }

                    await cmd.ExecuteNonQueryAsync();

                    bool success = (bool)resultadoParam.Value;
                    string message = (string)mensajeParam.Value;

                    if (success)
                    {
                        ShowMessage("Éxito", message, InfoBarSeverity.Success);
                        await Task.Delay(2000);
                        this.Close();
                    }
                    else
                    {
                        ShowMessage("Error al guardar", message, InfoBarSeverity.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error de Conexión", "No se pudo conectar a la base de datos: " + ex.Message, InfoBarSeverity.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private bool ValidateProductForm()
        {
            if (string.IsNullOrWhiteSpace(CodigoTextBox.Text))
            {
                ShowMessage("Campo Requerido", "El código del producto es obligatorio.", InfoBarSeverity.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(NombreTextBox.Text))
            {
                ShowMessage("Campo Requerido", "El nombre del producto es obligatorio.", InfoBarSeverity.Warning);
                return false;
            }
            if (PrecioVentaNumberBox.Value <= 0)
            {
                ShowMessage("Valor Inválido", "El precio de venta debe ser mayor que cero.", InfoBarSeverity.Warning);
                return false;
            }
            if (CantidadNumberBox.Value <= 0)
            {
                ShowMessage("Valor Inválido", "La cantidad debe ser mayor que cero.", InfoBarSeverity.Warning);
                return false;
            }
            return true;
        }

        private void ClearProductForm()
        {
            CodigoTextBox.Text = string.Empty;
            NombreTextBox.Text = string.Empty;
            CategoriaComboBox.SelectedIndex = -1;
            TallaTextBox.Text = string.Empty;
            ColorTextBox.Text = string.Empty;
            PrecioCompraNumberBox.Value = 0;
            PrecioVentaNumberBox.Value = 0;
            CantidadNumberBox.Value = 1;
            UnidadMedidaTextBox.Text = "Unidad";
            StockMinimoNumberBox.Value = 5;
            CodigoTextBox.Focus(FocusState.Programmatic);
        }
        
        private void ShowMessage(string title, string message, InfoBarSeverity severity)
        {
            NotificationBar.Title = title;
            NotificationBar.Message = message;
            NotificationBar.Severity = severity;
            NotificationBar.IsOpen = true;
        }
    }
}
