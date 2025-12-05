using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Graphics;

namespace ark_app1;

public sealed partial class MainPage : Window
{
    public AppWindow AppWindow { get; private set; }

    public MainPage(string userFullName)
    {
        this.InitializeComponent();
        
        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
        AppWindow = AppWindow.GetFromWindowId(windowId);

        AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
        AppWindow.Resize(new SizeInt32(1800, 1000));
        CenterWindow();
        UserPicture.DisplayName = userFullName;
        AppTitleBar.Subtitle = $"Bienvenido, {userFullName}";
        ContentFrame.Navigate(typeof(HomePage));
    }
     private void CenterWindow()
    {
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)?.WorkArea;
        if (area == null) return;
        AppWindow.Move(new PointInt32(
            (area.Value.Width - AppWindow.Size.Width) / 2,
            (area.Value.Height - AppWindow.Size.Height) / 2));
    }
    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var selectedItem = (NavigationViewItem)args.SelectedItem;
        if (selectedItem != null && selectedItem.Tag != null)
        {
            string selectedItemTag = selectedItem.Tag.ToString()!;

            // Navigate only if a valid page exists
            if (selectedItemTag == "Home" || selectedItemTag == "TermsAndConditions" || selectedItemTag == "AboutArkDev" || selectedItemTag == "Inventory" || selectedItemTag == "Sales" || selectedItemTag == "Providers" || selectedItemTag == "Clients")
            {
                NavView.Header = selectedItem.Content?.ToString() ?? string.Empty;
                string pageName = "ark_app1." + selectedItemTag + "Page";
                Type pageType = Type.GetType(pageName);
                ContentFrame.Navigate(pageType);
            }
        }
    }

    private void ProfileFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        NavView.Header = "Mi Perfil";
        ContentFrame.Navigate(typeof(ProfilePage));
    }

    private void LogoutFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        // You would typically navigate back to the login window and close this one.
        var mainWindow = new MainWindow();
        mainWindow.Activate();
        this.Close();
    }
}
