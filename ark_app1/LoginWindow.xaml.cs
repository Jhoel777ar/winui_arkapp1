using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using Microsoft.UI.Xaml.Controls;

namespace ark_app1;

public sealed partial class LoginWindow : Window
{
    public LoginWindow()
    {
        this.InitializeComponent();
        AppWindow.SetIcon("Assets/Tiles/GalleryIcon.ico");
        AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;

        // Center the window on the screen.
        CenterWindow();
        this.Closed += LoginWindow_Closed;
    }

    private void LoginWindow_Closed(object sender, WindowEventArgs args)
    {
        Application.Current.Exit();
    }

    private void CenterWindow()
    {
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)?.WorkArea;
        if (area == null) return;
        AppWindow.Move(new PointInt32((area.Value.Width - AppWindow.Size.Width) / 2, (area.Value.Height - AppWindow.Size.Height) / 2));
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Add your login logic here. 
        // For now, we'll just show an informational message.
        ShowInfoBar("Login exitoso", "Bienvenido!", InfoBarSeverity.Success);
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Add your registration logic here.
        // For now, we'll just show an informational message.
        ShowInfoBar("Registro exitoso", "Ahora puedes iniciar sesion.", InfoBarSeverity.Success);
        ShowLogin();
    }

    private void ShowRegisterButton_Click(object sender, RoutedEventArgs e)
    {
        ShowRegister();
    }

    private void ShowLoginButton_Click(object sender, RoutedEventArgs e)
    {
        ShowLogin();
    }

    private void ShowInfoBar(string title, string message, InfoBarSeverity severity)
    {
        LoginInfoBar.Title = title;
        LoginInfoBar.Message = message;
        LoginInfoBar.Severity = severity;
        LoginInfoBar.IsOpen = true;
    }

    private void ShowRegister()
    {
        LoginStackPanel.Visibility = Visibility.Collapsed;
        RegisterStackPanel.Visibility = Visibility.Visible;
        LoginInfoBar.IsOpen = false;
    }

    private void ShowLogin()
    {
        RegisterStackPanel.Visibility = Visibility.Collapsed;
        LoginStackPanel.Visibility = Visibility.Visible;
        LoginInfoBar.IsOpen = false;
    }
}
