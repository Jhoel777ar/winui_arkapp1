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
            this.AppWindow.Title = "Registrar Nueva Compra";

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
            this.AppWindow.Title = $"Editar Compra ID: {compraId}";
            TitleTextBlock.Text = $"Editar Compra ID: {compraId}";
            SaveCompraButton.Content = "Guardar Cambios";
        }

        private async void LoadCategorias()
        {
            try
            {
                var categorias = new List<object>();
                using (var conn = new SqlConnection(DatabaseManager.ConnectionString))
                {
                    await conn.OpenAsync();
                    var cmd = new SqlCommand("SELECT Id, Nombre FROM Categorias ORDER BY Nombre", conn);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            categorias.Add(new { Id = reader.GetInt32(0), Nombre = reader.GetString(1) });
                        }
                    }
                }
                CategoriaComboBox.ItemsSource = categorias;
                CategoriaComboBox.DisplayMemberPath = "Nombre";
                CategoriaComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                ShowMessage("Error al cargar categorías", ex.Message, InfoBarSeverity.Error);
            }
        }

        private async void SaveCompraButton_Click(object sender, RoutedEventArgs e)
        {
            if (_productosEnCompra.Count == 0)
            {
                ShowMessage("Validación", "Debe agregar al menos un producto.", InfoBarSeverity.Warning);
                return;
            }

            string jsonProductos = JsonSerializer.Serialize(_productosEnCompra.Select(p => new {
                ProductoId = p.Id, p.Codigo, p.Nombre, p.CategoriaId, p.Talla, p.Color,
                p.PrecioCompra, p.PrecioVenta, p.Cantidad, p.UnidadMedida, p.StockMinimo
            }));

            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
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

                await cmd.ExecuteNonQueryAsync();

                if ((bool)resultadoParam.Value)
                {
                    CompraSaved?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    ShowMessage("Error al guardar", (string)mensajeParam.Value, InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error de Conexión", ex.Message, InfoBarSeverity.Error);
            }
        }

        private void AddProductToListButton_Click(object sender, RoutedEventArgs e)
        {
            // ValidaciÃ³n y lÃ³gica para aÃ±adir producto a la lista _productosEnCompra
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
