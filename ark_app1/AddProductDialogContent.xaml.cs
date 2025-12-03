using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ark_app1
{
    public sealed partial class AddProductDialogContent : Window
    {
        private readonly ObservableCollection<Producto> _productosEnCompra = new();
        private bool _isEditMode = false;
        private int? _compraIdToEdit = null;

        public event EventHandler<EventArgs> CompraSaved;

        public AddProductDialogContent()
        {
            this.InitializeComponent();
            this.Title = "Registrar Nueva Compra";

            ProductosListView.ItemsSource = _productosEnCompra;
            SaveCompraButton.Click += SaveCompraButton_Click;
            CancelButton.Click += CancelButton_Click;
            AddProductToListButton.Click += AddProductToListButton_Click;
            
            LoadCategorias();
        }

        public AddProductDialogContent(int compraId) : this()
        {
            _isEditMode = true;
            _compraIdToEdit = compraId;
            this.Title = $"Editar Compra ID: {compraId}";
            TitleTextBlock.Text = $"Editar Compra ID: {compraId}";
            SaveCompraButton.Content = "Guardar Cambios";
            // AquÃ­ se deberÃ­an cargar los datos de la compra existente para la ediciÃ³n.
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
                ShowMessage("Error al cargar categorías", ex.Message, InfoBarSeverity.Error);
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
                ShowMessage("Validación", "Debe agregar al menos un producto a la compra.", InfoBarSeverity.Warning);
                return;
            }

            var productosParaJson = _productosEnCompra.Select(p => new {
                ProductoId = p.Id, p.Codigo, p.Nombre, p.CategoriaId, p.Talla, p.Color,
                p.PrecioCompra, p.PrecioVenta, p.Cantidad, p.UnidadMedida, p.StockMinimo
            });
            string jsonProductos = JsonSerializer.Serialize(productosParaJson);

            try
            {
                using var conn = await DatabaseManager.Instance.GetOpenConnectionAsync();
                var cmd = new SqlCommand(_isEditMode ? "sp_EditarCompra" : "sp_RegistrarCompra", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                if (_isEditMode)
                {
                    cmd.Parameters.AddWithValue("@CompraId", _compraIdToEdit.Value);
                }
                else
                {
                    var userId = (Application.Current as App)?.CurrentUser?.Id ?? 0;
                    cmd.Parameters.AddWithValue("@UsuarioId", userId);
                    cmd.Parameters.AddWithValue("@ProveedorId", DBNull.Value);
                }

                cmd.Parameters.AddWithValue("@Productos", jsonProductos);

                var resultadoParam = new SqlParameter("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                var mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(resultadoParam);
                cmd.Parameters.Add(mensajeParam);

                if (!_isEditMode)
                {
                    var compraIdParam = new SqlParameter("@CompraId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(compraIdParam);
                }

                await cmd.ExecuteNonQueryAsync();

                bool success = (bool)resultadoParam.Value;
                string message = (string)mensajeParam.Value;

                if (success)
                {
                    CompraSaved?.Invoke(this, EventArgs.Empty);
                    ShowMessage("Éxito", message, InfoBarSeverity.Success);
                    await Task.Delay(2000);
                    this.Close();
                }
                else
                {
                    ShowMessage("Error al guardar", message, InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error de Conexión", "No se pudo conectar: " + ex.Message, InfoBarSeverity.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private bool ValidateProductForm()
        {
            if (string.IsNullOrWhiteSpace(CodigoTextBox.Text)) { ShowMessage("Campo Requerido", "El código del producto es obligatorio.", InfoBarSeverity.Warning); return false; }
            if (string.IsNullOrWhiteSpace(NombreTextBox.Text)) { ShowMessage("Campo Requerido", "El nombre del producto es obligatorio.", InfoBarSeverity.Warning); return false; }
            if (PrecioVentaNumberBox.Value <= 0) { ShowMessage("Valor Inválido", "El precio de venta debe ser mayor que cero.", InfoBarSeverity.Warning); return false; }
            if (CantidadNumberBox.Value <= 0) { ShowMessage("Valor Inválido", "La cantidad debe ser mayor que cero.", InfoBarSeverity.Warning); return false; }
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
