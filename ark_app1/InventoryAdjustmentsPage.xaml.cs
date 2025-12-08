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
    public sealed partial class InventoryAdjustmentsPage : Page
    {
        public ObservableCollection<AdjustmentRecord> Records { get; } = new();
        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int PageSize = 20;

        public InventoryAdjustmentsPage()
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
                var cmd = new SqlCommand("sp_ObtenerHistorialAjustes", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Filtro", string.IsNullOrWhiteSpace(filter) ? (object)DBNull.Value : filter);
                cmd.Parameters.AddWithValue("@PageNumber", _currentPage);
                cmd.Parameters.AddWithValue("@PageSize", PageSize);

                using var r = await cmd.ExecuteReaderAsync();
                int totalRecords = 0;
                while (await r.ReadAsync())
                {
                    if (totalRecords == 0) totalRecords = r.GetInt32(r.GetOrdinal("TotalRegistros"));

                    Records.Add(new AdjustmentRecord
                    {
                        Id = r.GetInt32(r.GetOrdinal("Id")),
                        Fecha = r.GetDateTime(r.GetOrdinal("Fecha")),
                        Codigo = r.GetString(r.GetOrdinal("Codigo")),
                        Producto = r.GetString(r.GetOrdinal("Producto")),
                        Usuario = r.GetString(r.GetOrdinal("Usuario")),
                        Cantidad = r.GetDecimal(r.GetOrdinal("Cantidad")),
                        Motivo = r.GetString(r.GetOrdinal("Motivo"))
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
                // Simple error handling
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

    public class AdjustmentRecord
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}
