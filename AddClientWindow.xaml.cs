using System;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;

namespace LashAccountingSystem
{
    public partial class AddClientWindow : Window
    {
        public AddClientWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SurnameTextBox.Text))
            {
                MessageBox.Show("Введите фамилию клиента!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                SurnameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите имя клиента!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                MessageBox.Show("Введите номер телефона клиента!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PhoneTextBox.Focus();
                return;
            }

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    string sql = @"INSERT INTO clients 
                                  (client_surname, client_name, client_patronymic, 
                                   client_phone_number, client_email, client_health_features) 
                                  VALUES 
                                  (@surname, @name, @patronymic, @phone, @email, @health)";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        // Параметры (защита от SQL-инъекций)
                        cmd.Parameters.AddWithValue("@surname", SurnameTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@name", NameTextBox.Text.Trim());

                        if (string.IsNullOrWhiteSpace(PatronymicTextBox.Text))
                            cmd.Parameters.AddWithValue("@patronymic", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@patronymic", PatronymicTextBox.Text.Trim());

                        cmd.Parameters.AddWithValue("@phone", PhoneTextBox.Text.Trim());

                        if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                            cmd.Parameters.AddWithValue("@email", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@email", EmailTextBox.Text.Trim());

                        if (string.IsNullOrWhiteSpace(HealthTextBox.Text))
                            cmd.Parameters.AddWithValue("@health", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@health", HealthTextBox.Text.Trim());

                        cmd.ExecuteNonQuery();
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении клиента:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Закрываем окно без сохранения
            DialogResult = false;
            Close();
        }
    }
}