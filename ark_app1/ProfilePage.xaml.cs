using Microsoft.Data.SqlClient;
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
            LoadUserProfile();
        }

        private void LoadUserProfile()
        {
            var user = (Application.Current as App)?.CurrentUser;
            if (user != null)
            {
                FullNameTextBox.Text = user.NombreCompleto ?? string.Empty;
                CiTextBox.Text = user.CI ?? string.Empty;
                EmailTextBox.Text = user.Email ?? string.Empty;
                PhoneTextBox.Text = user.Telefono ?? string.Empty;
            }
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            var user = (Application.Current as App)?.CurrentUser;
            if (user == null) return;

            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Usuarios SET NombreCompleto = @nombre, Telefono = @tel WHERE Id = @id";
                cmd.Parameters.AddWithValue("@nombre", FullNameTextBox.Text.Trim());
                cmd.Parameters.AddWithValue("@tel", (object)PhoneTextBox.Text.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", user.Id);
                cmd.ExecuteNonQuery();

                user.NombreCompleto = FullNameTextBox.Text.Trim();
                user.Telefono = PhoneTextBox.Text.Trim();

                ShowInfoBar("Éxito", "Cambios guardados correctamente.", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowInfoBar("Error", $"Error al guardar: {ex.Message}", InfoBarSeverity.Error);
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

            var user = (Application.Current as App)?.CurrentUser;
            if (user == null) return;

            try
            {
                DatabaseManager.UpdateUserPassword(user.Id, NewPasswordBox.Password);
                NewPasswordBox.Password = string.Empty;
                ConfirmNewPasswordBox.Password = string.Empty;
                ShowInfoBar("Éxito", "Contraseña cambiada correctamente.", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowInfoBar("Error", $"Error al cambiar contraseña: {ex.Message}", InfoBarSeverity.Error);
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