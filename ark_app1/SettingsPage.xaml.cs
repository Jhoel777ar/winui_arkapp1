using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using WinRT;
using Microsoft.UI.Xaml.Media; // Agregado para corregir el error

namespace ark_app1
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
        }

        private void BackdropComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = (ComboBoxItem)e.AddedItems[0];
            string backdropType = selectedItem.Tag.ToString();

            var window = WindowManager.GetActiveWindow();
            if (window != null)
            {
                // Reset any existing backdrop
                window.SystemBackdrop = null;
                
                if (backdropType == "Mica")
                {
                    window.SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.Base };
                }
                else if (backdropType == "MicaAlt")
                {
                    window.SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.BaseAlt };
                }
                else if (backdropType == "Acrylic")
                {
                    window.SystemBackdrop = new DesktopAcrylicBackdrop();
                }
                // Note: ThinAcrylic is not a direct option in SystemBackdrop. A controller is needed for that.
            }
        }
    }

    public static class WindowManager
    {
        public static Window GetActiveWindow()
        {
            return App.Window;
        }
    }
}
