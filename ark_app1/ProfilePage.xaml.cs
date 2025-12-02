using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace ark_app1
{
    public sealed partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            this.InitializeComponent();
            LoadUserData();
        }

        private void LoadUserData()
        {
            // In a real app, you would load this data from a service or database
            FirstNameTextBox.Text = "John";
            LastNameTextBox.Text = "Doe";
            EmailTextBox.Text = "john.doe@example.com";
        }

        private async void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog savedDialog = new ContentDialog
            {
                Title = "Cambios Guardados",
                Content = "Tu información de perfil ha sido actualizada.",
                CloseButtonText = "Ok",
                XamlRoot = this.XamlRoot
            };
            await savedDialog.ShowAsync();
        }

        private async void UpdatePassword_Click(object sender, RoutedEventArgs e)
        {
            if (NewPasswordBox.Password != ConfirmNewPasswordBox.Password)
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Error de Contraseña",
                    Content = "Las nuevas contraseñas no coinciden.",
                    CloseButtonText = "Ok",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }
            
            if (string.IsNullOrEmpty(NewPasswordBox.Password))
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Error de Contraseña",
                    Content = "La nueva contraseña no puede estar vacía.",
                    CloseButtonText = "Ok",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }

            ContentDialog successDialog = new ContentDialog
            {
                Title = "Contraseña Actualizada",
                Content = "Tu contraseña ha sido cambiada exitosamente.",
                CloseButtonText = "Ok",
                XamlRoot = this.XamlRoot
            };
            await successDialog.ShowAsync();
        }

        private async void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog forgotPasswordDialog = new ContentDialog
            {
                Title = "Restablecer Contraseña",
                Content = "Se ha enviado una contraseña temporal a tu correo electrónico.",
                CloseButtonText = "Ok",
                XamlRoot = this.XamlRoot
            };
            await forgotPasswordDialog.ShowAsync();
        }
    }
}
