using BCrypt.Net;
using Microsoft.Data.SqlClient;
using System;

namespace ark_app1
{
    public static class DatabaseManager
    {
        private static string? _connectionString;
        public static string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    // Fallback or throw informative error
                     throw new InvalidOperationException("Database connection string has not been initialized.");
                }
                return _connectionString;
            }
            set => _connectionString = value;
        }

        public static void UpdateUserPassword(int userId, string newPassword)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Usuarios SET PasswordHash = @PasswordHash WHERE Id = @Id";
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                cmd.Parameters.AddWithValue("@Id", userId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
