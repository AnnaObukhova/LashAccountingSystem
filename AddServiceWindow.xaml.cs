using System;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;

namespace LashAccountingSystem
{
    public partial class AddServiceWindow : Window
    {
        public AddServiceWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите название услуги!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (!int.TryParse(DurationTextBox.Text, out int duration) || duration <= 0)
            {
                MessageBox.Show("Введите корректную длительность (положительное число)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                DurationTextBox.Focus();
                return;
            }

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    string sql = @"INSERT INTO services (service_name, service_description, service_duration) 
                                  VALUES (@name, @description, @duration)";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", NameTextBox.Text.Trim());

                        if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
                            cmd.Parameters.AddWithValue("@description", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@description", DescriptionTextBox.Text.Trim());

                        cmd.Parameters.AddWithValue("@duration", duration);

                        cmd.ExecuteNonQuery();
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении услуги:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}