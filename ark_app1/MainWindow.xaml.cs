using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Microsoft.Data.SqlClient;

namespace ark_app1
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            AppWindow.SetIcon("Assets/Tiles/GalleryIcon.ico");
            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;

            // Center the window on the screen.
            CenterWindow();
        }

        private void CenterWindow()
        {
            var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)?.WorkArea;
            if (area == null) return;
            AppWindow.Move(new PointInt32((area.Value.Width - AppWindow.Size.Width) / 2, (area.Value.Height - AppWindow.Size.Height) / 2));
        }

        private void AuthenticationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SqlAuthenticationStackPanel != null)
            {
                var selectedItem = (ComboBoxItem)AuthenticationComboBox.SelectedItem;
                if (selectedItem.Content.ToString() == "SQL Server Authentication")
                {
                    SqlAuthenticationStackPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    SqlAuthenticationStackPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            InfoBar.IsOpen = false;
            string serverName = ServerNameTextBox.Text;
            string connectionString;

            var selectedItem = (ComboBoxItem)AuthenticationComboBox.SelectedItem;
            if (selectedItem.Content.ToString() == "Windows Authentication")
            {
                connectionString = $"Server={serverName};Database=master;Integrated Security=True;TrustServerCertificate=True;";
            }
            else
            {
                string username = UsernameTextBox.Text;
                string password = PasswordBox.Password;
                connectionString = $"Server={serverName};Database=master;User ID={username};Password={password};TrustServerCertificate=True;";
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    InfoBar.Title = "Conexión exitosa";
                    InfoBar.Message = "La conexión al servidor SQL se ha establecido correctamente.";
                    InfoBar.Severity = InfoBarSeverity.Success;
                    InfoBar.IsOpen = true;
                }
                catch (Exception ex)
                {
                    InfoBar.Title = "Error de conexión";
                    InfoBar.Message = $"No se pudo establecer la conexión con el servidor SQL. Error: {ex.Message}";
                    InfoBar.Severity = InfoBarSeverity.Error;
                    InfoBar.IsOpen = true;
                }
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        { 
            // Lógica de conexión aquí 
        }
    }
}
