using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace ark_app1
{
    public sealed partial class SerialInputDialog : ContentDialog
    {
        public string SerialNumber { get; private set; } = string.Empty;

        public SerialInputDialog(string productName)
        {
            this.InitializeComponent();
            this.Title = "Número de Serie / Garantía";
            ProductNameBlock.Text = $"Ingrese S/N para: {productName}";
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SerialNumber = SerialBox.Text.Trim();
        }
    }
}
