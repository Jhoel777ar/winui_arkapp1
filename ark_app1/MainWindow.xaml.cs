using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using System.Data.SqlClient;

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
            string serverName = ServerNameTextBox.Text;
            string connectionString;

            var selectedItem = (ComboBoxItem)AuthenticationComboBox.SelectedItem;
            if (selectedItem.Content.ToString() == "Windows Authentication")
            {
                connectionString = $"Server={serverName};Database=master;Integrated Security=True;";
            }
            else
            {
                string username = UsernameTextBox.Text;
                string password = PasswordBox.Password;
                connectionString = $"Server={serverName};Database=master;User ID={username};Password={password};";
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    ContentDialog dialog = new ContentDialog
                    {
                        Title = "Conexión exitosa",
                        Content = "La conexión al servidor SQL se ha establecido correctamente.",
                        CloseButtonText = "Aceptar",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    ContentDialog dialog = new ContentDialog
                    {
                        Title = "Error de conexión",
                        Content = $"No se pudo establecer la conexión con el servidor SQL.\nError: {ex.Message}",
                        CloseButtonText = "Aceptar",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle connection logic here
        }
    }
}
