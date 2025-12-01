using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ark_app1
{
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        private bool _isInfoBarOpen;
        private string _infoBarTitle = string.Empty;
        private string _infoBarMessage = string.Empty;
        private InfoBarSeverity _infoBarSeverity = InfoBarSeverity.Informational;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsInfoBarOpen
        {
            get => _isInfoBarOpen;
            set => SetProperty(ref _isInfoBarOpen, value);
        }

        public string InfoBarTitle
        {
            get => _infoBarTitle;
            set => SetProperty(ref _infoBarTitle, value);
        }

        public string InfoBarMessage
        {
            get => _infoBarMessage;
            set => SetProperty(ref _infoBarMessage, value);
        }

        public InfoBarSeverity InfoBarSeverity
        {
            get => _infoBarSeverity;
            set => SetProperty(ref _infoBarSeverity, value);
        }

        public MainWindow()
        {
            this.InitializeComponent();
            AppWindow.SetIcon("Assets/Tiles/GalleryIcon.ico");
            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;         
            AppWindow.Resize(new SizeInt32(900, 800));
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
                SqlAuthenticationStackPanel.Visibility = (selectedItem.Content.ToString() == "SQL Server Authentication")
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            IsInfoBarOpen = false;
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
                    DatabaseManager.ConnectionString = connectionString;
                    InfoBarTitle = "Conexión exitosa";
                    InfoBarMessage = "La conexión al servidor SQL se ha establecido correctamente.";
                    InfoBarSeverity = InfoBarSeverity.Success;
                    IsInfoBarOpen = true;
                }
                catch (Exception ex)
                {
                    InfoBarTitle = "Error de conexión";
                    InfoBarMessage = $"No se pudo establecer la conexión con el servidor SQL. Error: {ex.Message}";
                    InfoBarSeverity = InfoBarSeverity.Error;
                    IsInfoBarOpen = true;
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
