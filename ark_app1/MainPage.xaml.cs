using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WinRT;

namespace ark_app1;

public sealed partial class MainPage : Window
{
    public MainPage(string userFullName)
    {
        this.InitializeComponent();
        UserPicture.DisplayName = userFullName;
        TitleBar.Subtitle = $"Bienvenido, {userFullName}";
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
            var configurationSource = new SystemBackdropConfiguration();
            this.Activated += (sender, args) =>
            {
                if (args.WindowActivationState != WindowActivationState.Deactivated)
                {
                    configurationSource.IsInputActive = true;
                }
                else
                {
                    configurationSource.IsInputActive = false;
                }
            };
            this.Closed += (sender, args) =>
            {
                micaController?.Dispose();
                micaController = null;
            };

            ((FrameworkElement)this.Content).ActualThemeChanged += (sender, args) =>
            {
                if (configurationSource != null)
                {
                    SetConfigurationSourceTheme();
                }
            };

            configurationSource.IsInputActive = true;
            SetConfigurationSourceTheme();
            var micaController = new MicaController();
            micaController.Kind = MicaKind.BaseAlt;
            micaController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
            micaController.SetSystemBackdropConfiguration(configurationSource);

            return true;
        }
        return false;
    }
    MicaController? micaController;

    private void SetConfigurationSourceTheme()
    {
        // Implement this method to set the theme for the backdrop
    }
}