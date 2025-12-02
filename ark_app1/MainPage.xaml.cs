using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WinRT;

namespace ark_app1;

public sealed partial class MainPage : Window
{
    MicaController? micaController;
    SystemBackdropConfiguration? configurationSource;
    public MainPage(string userFullName)
    {
        this.InitializeComponent();
        UserPicture.DisplayName = userFullName;
        AppTitleBar.Subtitle = $"Bienvenido, {userFullName}";
        TrySetMicaBackdrop();
        ContentFrame.Navigate(typeof(HomePage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
        }
        else
        {
            var selectedItem = (NavigationViewItem)args.SelectedItem;
            if (selectedItem != null)
            {
                string selectedItemTag = ((string)selectedItem.Tag);
                NavView.Header = selectedItemTag;
                string pageName = "ark_app1." + selectedItemTag + "Page";
                Type pageType = Type.GetType(pageName);
                ContentFrame.Navigate(pageType);
            }
        }
    }

    bool TrySetMicaBackdrop()
    {
        if (MicaController.IsSupported())
        {
            configurationSource = new SystemBackdropConfiguration();
            this.Activated += Window_Activated;
            this.Closed += Window_Closed;
            ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;
            configurationSource.IsInputActive = true;
            SetConfigurationSourceTheme();
            micaController = new MicaController();
            micaController.Kind = MicaKind.BaseAlt;
            micaController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
            micaController.SetSystemBackdropConfiguration(configurationSource);

            return true;
        }
        return false;
    }
    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (configurationSource != null)
            configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        micaController?.Dispose();
        micaController = null;
        configurationSource = null;
        this.Activated -= Window_Activated;
        this.Closed -= Window_Closed;
        ((FrameworkElement)this.Content).ActualThemeChanged -= Window_ThemeChanged;

    }

    private void Window_ThemeChanged(FrameworkElement sender, object args)
    {
        if (configurationSource != null)
            SetConfigurationSourceTheme();
    }

    private void SetConfigurationSourceTheme()
    {
        if (configurationSource == null) return;

        switch (((FrameworkElement)this.Content).ActualTheme)
        {
            case ElementTheme.Dark:
                configurationSource.Theme = SystemBackdropTheme.Dark;
                break;
            case ElementTheme.Light:
                configurationSource.Theme = SystemBackdropTheme.Light;
                break;
            case ElementTheme.Default:
                configurationSource.Theme = SystemBackdropTheme.Default;
                break;
        }
    }
}
