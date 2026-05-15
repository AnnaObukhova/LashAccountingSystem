using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class ServicesWindow : Window
    {
        private List<Service> _allServices;
        private List<Service> _filteredServices;

        public ServicesWindow()
        {
            InitializeComponent();
            LoadServices();
        }

        private void LoadServices(string searchText = "")
        {
            try
            {
                _allServices = new List<Service>();

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
                            _allServices.Add(new Service
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

                ApplyFilter(searchText);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки услуг: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredServices = _allServices;
            }
            else
            {
                string lowerSearch = searchText.ToLower();
                _filteredServices = _allServices.Where(s =>
                    (s.ServiceName?.ToLower().Contains(lowerSearch) ?? false) ||
                    (s.ServiceDescription?.ToLower().Contains(lowerSearch) ?? false)
                ).ToList();
            }

            ServicesDataGrid.ItemsSource = _filteredServices;

            if (SearchResultCount != null)
            {
                SearchResultCount.Text = $"Найдено: {_filteredServices.Count} из {_allServices.Count}";
            }
        }

        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplyFilter(SearchTextBox?.Text ?? "");
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox != null)
            {
                SearchTextBox.Text = "";
            }
            ApplyFilter("");
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddServiceWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadServices(SearchTextBox?.Text ?? "");
                MessageBox.Show("Услуга успешно добавлена!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (ServicesDataGrid.SelectedItem is Service selectedService)
            {
                var editWindow = new EditServiceWindow(selectedService);
                if (editWindow.ShowDialog() == true)
                {
                    LoadServices(SearchTextBox?.Text ?? "");
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
                        LoadServices(SearchTextBox?.Text ?? "");
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

        private void PriceHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var priceHistoryWindow = new PriceHistoryWindow();
            priceHistoryWindow.Owner = this;
            priceHistoryWindow.ShowDialog();
        }
    }
}