using System;
using System.Windows;
using LashAccountingSystem.Database;
using LashAccountingSystem.Security;
using LashAccountingSystem.Models;
using Npgsql;

namespace LashAccountingSystem
{
    public partial class LoginWindow : Window
    {
        public bool IsLoggedIn { get; private set; } = false;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите логин и пароль!");
                return;
            }

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT user_id, password_hash, salt, is_active FROM users WHERE login = @login";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = reader.GetInt32(0);
                                string storedHash = reader.GetString(1);
                                string salt = reader.GetString(2);
                                bool isActive = reader.GetBoolean(3);

                                if (!isActive)
                                {
                                    ShowError("Учётная запись заблокирована!");
                                    return;
                                }

                                if (PasswordHasher.VerifyPassword(password, storedHash, salt))
                                {
                                    App.CurrentUser = new User
                                    {
                                        UserId = userId,
                                        Login = login,
                                        IsActive = isActive
                                    };
                                    IsLoggedIn = true;
                                    DialogResult = true;
                                    Close();
                                    return;
                                }
                                else
                                {
                                    ShowError("Неверный пароль!");
                                }
                            }
                            else
                            {
                                ShowError("Пользователь не найден!");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }
    }
}