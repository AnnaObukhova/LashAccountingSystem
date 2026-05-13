using System;
using System.Collections.Generic;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class ServicesWindow : Window
    {
        public ServicesWindow()
        {
            InitializeComponent();
            // Owner = Application.Current.MainWindow;
            LoadServices();
        }

        private void LoadServices()
        {
            List<Service> services = new List<Service>();

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                SELECT 
                    s.service_id,
                    s.service_name,
                    s.service_description,
                    s.service_duration,
                    COALESCE(ph.price_history_price, 0) as current_price
                FROM services s
                LEFT JOIN LATERAL (
                    SELECT price_history_price 
                    FROM price_history 
                    WHERE service_id = s.service_id 
                    ORDER BY price_history_application_date DESC 
                    LIMIT 1
                ) ph ON true
                ORDER BY s.service_name";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            services.Add(new Service
                            {
                                ServiceId = reader.GetInt32(0),
                                ServiceName = reader.GetString(1),
                                ServiceDescription = reader.IsDBNull(2) ? null : reader.GetString(2),
                                ServiceDuration = reader.GetInt32(3),
                                CurrentPrice = reader.GetDecimal(4)
                            });
                        }
                    }
                }

                ServicesDataGrid.ItemsSource = services;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки услуг: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddServiceWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadServices();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (ServicesDataGrid.SelectedItem is Service selectedService)
            {
                var editWindow = new EditServiceWindow(selectedService);
                if (editWindow.ShowDialog() == true)
                {
                    LoadServices();
                    MessageBox.Show("Услуга успешно обновлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Выберите услугу для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ServicesDataGrid.SelectedItem is Service selectedService)
            {
                var result = MessageBox.Show($"Удалить услугу \"{selectedService.ServiceName}\"?\n\n" +
                    "Внимание! Будут удалены также связанные записи и история цен.",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var conn = DbConnection.GetConnection())
                        {
                            conn.Open();
                            string sql = "DELETE FROM services WHERE service_id = @id";
                            using (var cmd = new NpgsqlCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", selectedService.ServiceId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        LoadServices();
                        MessageBox.Show("Услуга удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите услугу для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ServicesDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ServicesDataGrid.SelectedItem is Service selectedService)
            {
                var detailsWindow = new ServiceDetailsWindow(selectedService);
                detailsWindow.ShowDialog();
            }
        }
    }
}