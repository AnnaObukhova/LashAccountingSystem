using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class MainWindow : Window
    {
        private List<Client> _allClients;
        private List<Client> _filteredClients;

        public MainWindow()
        {
            InitializeComponent();
            LoadClients();
        }

        private void LoadClients(string searchText = "")
        {
            try
            {
                _allClients = new List<Client>();

                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT client_id, client_surname, client_name, client_patronymic, " +
                                 "client_phone_number, client_email, client_health_features " +
                                 "FROM clients ORDER BY client_surname;";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _allClients.Add(new Client
                            {
                                ClientId = reader.GetInt32(0),
                                ClientSurname = reader.GetString(1),
                                ClientName = reader.GetString(2),
                                ClientPatronymic = reader.IsDBNull(3) ? null : reader.GetString(3),
                                ClientPhoneNumber = reader.GetString(4),
                                ClientEmail = reader.IsDBNull(5) ? null : reader.GetString(5),
                                ClientHealthFeatures = reader.IsDBNull(6) ? null : reader.GetString(6)
                            });
                        }
                    }
                }

                ApplyFilter(searchText);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredClients = _allClients;
            }
            else
            {
                string lowerSearch = searchText.ToLower();
                _filteredClients = _allClients.Where(c =>
                    (c.ClientSurname?.ToLower().Contains(lowerSearch) ?? false) ||
                    (c.ClientName?.ToLower().Contains(lowerSearch) ?? false) ||
                    (c.ClientPatronymic?.ToLower().Contains(lowerSearch) ?? false) ||
                    (c.ClientPhoneNumber?.Contains(lowerSearch) ?? false) ||
                    (c.ClientEmail?.ToLower().Contains(lowerSearch) ?? false)
                ).ToList();
            }

            ClientsDataGrid.ItemsSource = _filteredClients;

            if (SearchResultCount != null)
            {
                SearchResultCount.Text = $"Найдено: {_filteredClients.Count} из {_allClients.Count}";
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
            var addWindow = new AddClientWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadClients(SearchTextBox?.Text ?? "");
                MessageBox.Show("Клиент успешно добавлен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is Client selectedClient)
            {
                var editWindow = new EditClientWindow(selectedClient);
                if (editWindow.ShowDialog() == true)
                {
                    LoadClients(SearchTextBox?.Text ?? "");
                    MessageBox.Show("Данные клиента успешно обновлены!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Выберите клиента для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is Client selectedClient)
            {
                var result = MessageBox.Show($"Удалить клиента {selectedClient.ClientSurname} {selectedClient.ClientName}?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var conn = DbConnection.GetConnection())
                        {
                            conn.Open();
                            string sql = "DELETE FROM clients WHERE client_id = @id";
                            using (var cmd = new NpgsqlCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", selectedClient.ClientId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        LoadClients(SearchTextBox?.Text ?? "");
                        MessageBox.Show("Клиент удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Выберите клиента для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ClientsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is Client selectedClient)
            {
                var cardWindow = new ClientCardWindow(selectedClient.ClientId, selectedClient.ClientName);
                cardWindow.ShowDialog();
            }
        }
    }
}