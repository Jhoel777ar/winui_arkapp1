using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.Data.SqlClient;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;

namespace ark_app1
{
    public sealed partial class SalesHistoryPage : Page
    {
        public ObservableCollection<SalesHistoryRecord> Records { get; } = new();
        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int PageSize = 20;

        public SalesHistoryPage()
        {
            this.InitializeComponent();
            this.Loaded += (s, e) => _ = LoadData();
        }

        private async Task LoadData(string filter = "")
        {
            Records.Clear();
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();
                var cmd = new SqlCommand("sp_ObtenerHistorialVentas", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Filtro", string.IsNullOrWhiteSpace(filter) ? (object)DBNull.Value : filter);
                cmd.Parameters.AddWithValue("@PageNumber", _currentPage);
                cmd.Parameters.AddWithValue("@PageSize", PageSize);

                using var r = await cmd.ExecuteReaderAsync();
                int totalRecords = 0;
                while (await r.ReadAsync())
                {
                    if (totalRecords == 0) totalRecords = r.GetInt32(r.GetOrdinal("TotalRegistros"));

                    Records.Add(new SalesHistoryRecord
                    {
                        VentaId = r.GetInt32(r.GetOrdinal("VentaId")),
                        Fecha = r.GetDateTime(r.GetOrdinal("Fecha")),
                        Usuario = r.GetString(r.GetOrdinal("Usuario")),
                        Cliente = r.GetString(r.GetOrdinal("Cliente")),
                        TotalVenta = r.GetDecimal(r.GetOrdinal("Total")),
                        DescuentoPorcentaje = r.IsDBNull(r.GetOrdinal("DescuentoPorcentaje")) ? 0 : r.GetDecimal(r.GetOrdinal("DescuentoPorcentaje")),
                        DescuentoMonto = r.IsDBNull(r.GetOrdinal("DescuentoMonto")) ? 0 : r.GetDecimal(r.GetOrdinal("DescuentoMonto")),
                        EfectivoRecibido = r.IsDBNull(r.GetOrdinal("EfectivoRecibido")) ? 0 : r.GetDecimal(r.GetOrdinal("EfectivoRecibido")),
                        Cambio = r.IsDBNull(r.GetOrdinal("Cambio")) ? 0 : r.GetDecimal(r.GetOrdinal("Cambio")),
                        Estado = r.GetString(r.GetOrdinal("Estado")),
                        TipoPago = r.GetString(r.GetOrdinal("TipoPago")),
                        DetalleId = r.GetInt32(r.GetOrdinal("DetalleId")),
                        CodigoProducto = r.GetString(r.GetOrdinal("CodigoProducto")),
                        Producto = r.GetString(r.GetOrdinal("Producto")),
                        Cantidad = r.GetDecimal(r.GetOrdinal("Cantidad")),
                        PrecioUnitario = r.GetDecimal(r.GetOrdinal("PrecioUnitario")),
                        DescuentoDetallePorc = r.IsDBNull(r.GetOrdinal("DescuentoDetallePorc")) ? 0 : r.GetDecimal(r.GetOrdinal("DescuentoDetallePorc")),
                        DescuentoDetalleMonto = r.IsDBNull(r.GetOrdinal("DescuentoDetalleMonto")) ? 0 : r.GetDecimal(r.GetOrdinal("DescuentoDetalleMonto")),
                        Subtotal = r.GetDecimal(r.GetOrdinal("Subtotal"))
                    });
                }

                _totalPages = (int)Math.Ceiling((double)totalRecords / PageSize);
                if (_totalPages < 1) _totalPages = 1;
                PageInfo.Text = $"Página {_currentPage} de {_totalPages}";

                PrevButton.IsEnabled = _currentPage > 1;
                NextButton.IsEnabled = _currentPage < _totalPages;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentPage = 1;
            _ = LoadData(SearchBox.Text);
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                _ = LoadData(SearchBox.Text);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                _ = LoadData(SearchBox.Text);
            }
        }
    }

    public class SalesHistoryRecord
    {
        public int VentaId { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public decimal TotalVenta { get; set; }
        public decimal DescuentoPorcentaje { get; set; }
        public decimal DescuentoMonto { get; set; }
        public decimal EfectivoRecibido { get; set; }
        public decimal Cambio { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string TipoPago { get; set; } = string.Empty;

        public int DetalleId { get; set; }
        public string CodigoProducto { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal DescuentoDetallePorc { get; set; }
        public decimal DescuentoDetalleMonto { get; set; }
        public decimal Subtotal { get; set; }

        public string PrecioUnitarioDisplay => $"Bs. {PrecioUnitario:N2}";
        public string SubtotalDisplay => $"Bs. {Subtotal:N2}";
        public string TotalVentaDisplay => $"Bs. {TotalVenta:N2}";
    }
}
