using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Data.Sqlite;
using System;

namespace ark_app1
{
    public sealed partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            this.InitializeComponent();
            LoadUserProfile();
        }

        private void LoadUserProfile()
        {
            var user = (Application.Current as App).CurrentUser;
            if (user != null)
            {
                FullNameTextBox.Text = user.NombreCompleto;
                CiTextBox.Text = user.CI;
                EmailTextBox.Text = user.Email;
                PhoneTextBox.Text = user.Telefono;
            }
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            var user = (Application.Current as App).CurrentUser;
            if (user != null)
            {
                try
                {
                    using (var db = new SqliteConnection($"Filename={DatabaseManager.DatabasePath}"))
                    {
                        db.Open();
                        var updateCmd = db.CreateCommand();
                        updateCmd.CommandText = "UPDATE Usuarios SET Telefono = @Telefono WHERE Id = @Id";
                        updateCmd.Parameters.AddWithValue("@Telefono", PhoneTextBox.Text);
                        updateCmd.Parameters.AddWithValue("@Id", user.Id);
                        updateCmd.ExecuteNonQuery();
                    }
                    user.Telefono = PhoneTextBox.Text;

                    ShowInfoBar("Éxito", "Sus cambios se han guardado exitosamente.", InfoBarSeverity.Success);
                }
                catch (Exception ex)
                {
                    ShowInfoBar("Error", $"No se pudieron guardar los cambios: {ex.Message}", InfoBarSeverity.Error);
                }
            }
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (NewPasswordBox.Password != ConfirmNewPasswordBox.Password)
            {
                ShowInfoBar("Error", "Las contraseñas no coinciden.", InfoBarSeverity.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
            {
                ShowInfoBar("Error", "La contraseña no puede estar vacía.", InfoBarSeverity.Error);
                return;
            }

            var user = (Application.Current as App).CurrentUser;
            if (user != null)
            {
                try
                {
                    DatabaseManager.UpdateUserPassword(user.Id, NewPasswordBox.Password);
                    ShowInfoBar("Éxito", "Su contraseña ha sido cambiada exitosamente.", InfoBarSeverity.Success);
                    NewPasswordBox.Password = string.Empty;
                    ConfirmNewPasswordBox.Password = string.Empty;
                }
                catch (Exception ex)
                {
                    ShowInfoBar("Error", $"No se pudo cambiar la contraseña: {ex.Message}", InfoBarSeverity.Error);
                }
            }
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            var user = (Application.Current as App).CurrentUser;
            if (user != null)
            {
                try
                {
                    DatabaseManager.UpdateUserPassword(user.Id, user.CI);
                    ShowInfoBar("Información", "Su contraseña ha sido restablecida a su CI. Por favor, cambie su contraseña lo antes posible.", InfoBarSeverity.Informational);
                }
                catch (Exception ex)
                {
                    ShowInfoBar("Error", $"No se pudo restablecer la contraseña: {ex.Message}", InfoBarSeverity.Error);
                }
            }
        }

        private void ShowInfoBar(string title, string message, InfoBarSeverity severity)
        {
            InfoBar.Title = title;
            InfoBar.Message = message;
            InfoBar.Severity = severity;
            InfoBar.IsOpen = true;
        }
    }
}
