using System;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Security;
using LashAccountingSystem.Models;

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
                                bool isActive = reader.GetBoolean(3);
                                if (!isActive)
                                {
                                    ShowError("Учётная запись заблокирована!");
                                    return;
                                }

                                string storedHash = reader.GetString(1);
                                string salt = reader.GetString(2);

                                if (PasswordHasher.VerifyPassword(password, storedHash, salt))
                                {
                                    App.CurrentUser = new User
                                    {
                                        UserId = reader.GetInt32(0),
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
                ShowError($"Ошибка подключения к базе данных: {ex.Message}");
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.ShowDialog();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }
    }
}