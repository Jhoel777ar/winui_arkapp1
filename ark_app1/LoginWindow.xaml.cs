using BCrypt.Net;
using Microsoft.Data.SqlClient;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics;

namespace ark_app1;

public sealed partial class LoginWindow : Window
{
    private readonly Regex _emailRegex = new(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", RegexOptions.Compiled);
    private readonly Regex _numericRegex = new(@"^\d+$", RegexOptions.Compiled);

    public LoginWindow()
    {
        this.InitializeComponent();
        AppWindow.SetIcon("Assets/Tiles/GalleryIcon.ico");
        AppWindow.Resize(new SizeInt32(700, 950));
        CenterWindow();
        EnsureCorrectDatabase();
        this.Closed += LoginWindow_Closed;
    }

    private void EnsureCorrectDatabase()
    {
        if (DatabaseManager.ConnectionString?.Contains("Database=master") == true)
            DatabaseManager.ConnectionString = DatabaseManager.ConnectionString.Replace("Database=master", "Database=arkdbsisventas");
    }

    private void CenterWindow()
    {
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)?.WorkArea;
        if (area == null) return;
        AppWindow.Move(new PointInt32(
            (area.Value.Width - AppWindow.Size.Width) / 2,
            (area.Value.Height - AppWindow.Size.Height) / 2));
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        string cred = LoginCredentialTextBox.Text.Trim();
        string pass = LoginPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(cred) || string.IsNullOrWhiteSpace(pass))
        {
            ShowInfoBar("Error", "Completa ambos campos para iniciar sesión.", InfoBarSeverity.Error);
            return;
        }

        await Task.Run(async () =>
        {
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();

                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT PasswordHash, NombreCompleto FROM Usuarios WHERE Email = @cred OR CI = @cred";
                cmd.Parameters.AddWithValue("@cred", cred);

                using var reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    string hash = reader.GetString(0);
                    string nombre = reader.GetString(1);

                    if (BCrypt.Net.BCrypt.Verify(pass, hash))
                    {
                        DispatcherQueue.TryEnqueue(() =>
                            ShowInfoBar("¡Bienvenido!", $"Hola, {nombre} 👋", InfoBarSeverity.Success));
                    }
                    else
                    {
                        DispatcherQueue.TryEnqueue(() => ShowInfoBar("Error", "Contraseña incorrecta.", InfoBarSeverity.Error));
                    }
                }
                else
                {
                    DispatcherQueue.TryEnqueue(() => ShowInfoBar("Error", "Usuario no encontrado.", InfoBarSeverity.Error));
                }
            }
            catch
            {
                DispatcherQueue.TryEnqueue(() => ShowInfoBar("Error", "No se pudo conectar a la base de datos.", InfoBarSeverity.Error));
            }
        });
    }
    private async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        string nombre = FullNameTextBox.Text.Trim();
        string telefono = PhoneTextBox.Text.Trim();
        string ci = CiTextBox.Text.Trim();
        string email = EmailTextBox.Text.Trim();
        string password = RegisterPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(ci) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            ShowInfoBar("Error", "Todos los campos son obligatorios.", InfoBarSeverity.Error);
            return;
        }

        if (!_emailRegex.IsMatch(email))
        {
            ShowInfoBar("Error", "Ingresa un correo electrónico válido.", InfoBarSeverity.Warning);
            return;
        }

        if (!_numericRegex.IsMatch(ci) || ci.Length < 7 || ci.Length > 8)
        {
            ShowInfoBar("Error", "La CI debe contener solo números (7 u 8 dígitos).", InfoBarSeverity.Warning);
            return;
        }

        if (!string.IsNullOrEmpty(telefono) && !_numericRegex.IsMatch(telefono))
        {
            ShowInfoBar("Error", "El teléfono solo debe contener números.", InfoBarSeverity.Warning);
            return;
        }

        if (password.Length < 6)
        {
            ShowInfoBar("Error", "La contraseña debe tener al menos 6 caracteres.", InfoBarSeverity.Warning);
            return;
        }

        string hash = BCrypt.Net.BCrypt.HashPassword(password);

        await Task.Run(async () =>
        {
            try
            {
                using var conn = new SqlConnection(DatabaseManager.ConnectionString);
                await conn.OpenAsync();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Usuarios (NombreCompleto, CI, Email, Telefono, PasswordHash)
                    VALUES (@nombre, @ci, @email, @tel, @hash)";

                cmd.Parameters.AddWithValue("@nombre", nombre);
                cmd.Parameters.AddWithValue("@ci", ci);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@tel", telefono ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@hash", hash);

                await cmd.ExecuteNonQueryAsync();

                DispatcherQueue.TryEnqueue(() =>
                {
                    ShowInfoBar("¡Perfecto!", "Cuenta creada correctamente. Ya puedes iniciar sesión.", InfoBarSeverity.Success);
                    ShowLogin();
                });
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                DispatcherQueue.TryEnqueue(() => ShowInfoBar("Error", "El CI o Email ya está registrado.", InfoBarSeverity.Error));
            }
            catch (Exception)
            {
                DispatcherQueue.TryEnqueue(() => ShowInfoBar("Error", "Error al crear la cuenta.", InfoBarSeverity.Error));
            }
        });
    }
    private void ShowRegisterButton_Click(object sender, RoutedEventArgs e) => ShowRegister();
    private void ShowLoginButton_Click(object sender, RoutedEventArgs e) => ShowLogin();

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
    private void LoginWindow_Closed(object sender, WindowEventArgs args) => Application.Current.Exit();
}