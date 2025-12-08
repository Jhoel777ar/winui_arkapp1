using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.Data.SqlClient;
using System.Data;
using System;
using System.Threading.Tasks;

namespace ark_app1
{
    public sealed partial class CashCutDialog : ContentDialog
    {
        public CashCutDialog()
        {
            this.InitializeComponent();
            this.Loaded += CashCutDialog_Loaded;
        }

        private async void CashCutDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            LoadingRing.IsActive = true;
            StatsPanel.Visibility = Visibility.Collapsed;
            ErrorBlock.Visibility = Visibility.Collapsed;

            try
            {
                int userId = (Application.Current as App)?.CurrentUser?.Id ?? 0;
                if (userId == 0) throw new Exception("Usuario no identificado");

                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();

                var cmd = new SqlCommand("sp_ObtenerArqueoCaja", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UsuarioId", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    // Result Set 1: Totals
                    UserBlock.Text = reader["Usuario"].ToString();
                    DateBlock.Text = DateTime.Now.ToString("D"); // Today

                    decimal total = reader.GetDecimal(reader.GetOrdinal("TotalVendido"));
                    decimal cash = reader.GetDecimal(reader.GetOrdinal("TotalEfectivo"));
                    decimal card = reader.GetDecimal(reader.GetOrdinal("TotalTarjeta"));
                    decimal qr = reader.GetDecimal(reader.GetOrdinal("TotalQR"));
                    decimal transfer = reader.GetDecimal(reader.GetOrdinal("TotalTransferencia"));
                    int count = reader.GetInt32(reader.GetOrdinal("CantidadVentas"));

                    TotalSalesBlock.Text = $"Bs. {total:N2}";
                    CashBlock.Text = $"Bs. {cash:N2}";
                    CardBlock.Text = $"Bs. {card:N2}";
                    QrBlock.Text = $"Bs. {qr:N2}";
                    TransferBlock.Text = $"Bs. {transfer:N2}";
                    CountBlock.Text = $"{count} Ventas";
                }

                // Result Set 2 (Optional history) could be loaded here if we added a DataGrid

                StatsPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ErrorBlock.Text = "Error al cargar arqueo: " + ex.Message;
                ErrorBlock.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}
