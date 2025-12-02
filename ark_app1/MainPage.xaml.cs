using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace ark_app1;

public sealed partial class MainPage : Window
{
    public MainPage(string userFullName)
    {
        this.InitializeComponent();
        UserPicture.DisplayName = userFullName;
        AppTitleBar.Subtitle = $"Bienvenido, {userFullName}";
        ContentFrame.Navigate(typeof(HomePage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
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

    private void ProfileFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(ProfilePage));
    }

    private void LogoutFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement proper logout logic (e.g., return to login screen)
        this.Close();
    }
}
