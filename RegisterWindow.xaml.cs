using System;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Security;

namespace LashAccountingSystem
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            // Проверка заполнения
            if (string.IsNullOrWhiteSpace(login))
            {
                ShowError("Введите логин!");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите пароль!");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Пароли не совпадают!");
                return;
            }

            // Проверка сложности пароля
            if (!IsPasswordComplex(password))
            {
                ShowError("Пароль должен содержать минимум 6 символов, цифры и буквы!");
                return;
            }

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    // Проверка существования логина
                    string checkSql = "SELECT COUNT(*) FROM users WHERE login = @login";
                    using (var cmd = new NpgsqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        long count = (long)cmd.ExecuteScalar();
                        if (count > 0)
                        {
                            ShowError("Пользователь с таким логином уже существует!");
                            return;
                        }
                    }

                    // Создание пользователя
                    string salt = PasswordHasher.GenerateSalt();
                    string passwordHash = PasswordHasher.HashPassword(password, salt);

                    string insertSql = @"INSERT INTO users (login, password_hash, salt, registration_date, is_active) 
                                        VALUES (@login, @hash, @salt, @date, @active)";

                    using (var cmd = new NpgsqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@hash", passwordHash);
                        cmd.Parameters.AddWithValue("@salt", salt);
                        cmd.Parameters.AddWithValue("@date", DateTime.Today);
                        cmd.Parameters.AddWithValue("@active", true);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Регистрация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
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

        private bool IsPasswordComplex(string password)
        {
            if (password.Length < 6) return false;
            bool hasDigit = false;
            bool hasLetter = false;
            foreach (char c in password)
            {
                if (char.IsDigit(c)) hasDigit = true;
                if (char.IsLetter(c)) hasLetter = true;
            }
            return hasDigit && hasLetter;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }
    }
}