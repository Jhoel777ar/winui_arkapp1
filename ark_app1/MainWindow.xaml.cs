using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;


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

        DesktopAcrylicController? acrylicController;
        SystemBackdropConfiguration? configurationSource;

        public MainWindow()
        {
            this.InitializeComponent();
            AppWindow.SetIcon("Assets/Tiles/GalleryIcon.ico");
            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;

            AppWindow.Resize(new SizeInt32(700, 800));

            CenterWindow();
            TrySetAcrylicBackdrop();
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
                    
                    var loginWindow = new LoginWindow();
                    loginWindow.Activate();
                    this.Close();

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

        bool TrySetAcrylicBackdrop()
        {
            if (DesktopAcrylicController.IsSupported())
            {
                configurationSource = new SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;
                configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();
                acrylicController = new DesktopAcrylicController();
                acrylicController.Kind = DesktopAcrylicKind.Base;
                acrylicController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
                acrylicController.SetSystemBackdropConfiguration(configurationSource);
                return true; 
            }

            return false;
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (configurationSource is not null)
            {
                configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
            }
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            if (acrylicController != null)
            {
                acrylicController.Dispose();
                acrylicController = null;
            }
            this.Activated -= Window_Activated;
            configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            if (configurationSource is not null)
            {
                switch (((FrameworkElement)this.Content).ActualTheme)
                {
                    case ElementTheme.Dark: configurationSource.Theme = SystemBackdropTheme.Dark; break;
                    case ElementTheme.Light: configurationSource.Theme = SystemBackdropTheme.Light; break;
                    case ElementTheme.Default: configurationSource.Theme = SystemBackdropTheme.Default; break;
                }
            }
        }
    }
}
