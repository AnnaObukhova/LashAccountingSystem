using System;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class EditServiceWindow : Window
    {
        private Service _service;

        public EditServiceWindow(Service service)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            _service = service;
            LoadServiceData();
        }

        private void LoadServiceData()
        {
            IdTextBox.Text = _service.ServiceId.ToString();
            NameTextBox.Text = _service.ServiceName;
            DurationTextBox.Text = _service.ServiceDuration.ToString();
            DescriptionTextBox.Text = _service.ServiceDescription ?? "";
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

                    string sql = @"UPDATE services SET 
                                  service_name = @name,
                                  service_description = @description,
                                  service_duration = @duration
                                  WHERE service_id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _service.ServiceId);
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