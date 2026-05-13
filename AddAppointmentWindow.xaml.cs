using System;
using System.Collections.Generic;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using System.Windows.Controls;

namespace LashAccountingSystem
{
    public partial class AddAppointmentWindow : Window
    {
        private DateTime _defaultDate;
        private Dictionary<int, decimal> _servicePrices = new Dictionary<int, decimal>();

        public AddAppointmentWindow(DateTime defaultDate)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            _defaultDate = defaultDate;
            DatePicker.SelectedDate = defaultDate;
            LoadClients();
            LoadServices();
            LoadMasters();

            // Добавляем обработчик для создания нового клиента
            ClientComboBox.LostFocus += ClientComboBox_LostFocus;
        }

        private void ClientComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string enteredText = ClientComboBox.Text;

            // Если текст не пустой и не выбран существующий клиент
            if (!string.IsNullOrWhiteSpace(enteredText) && ClientComboBox.SelectedItem == null)
            {
                var result = MessageBox.Show($"Клиент \"{enteredText}\" не найден.\n\nСоздать нового клиента с таким именем?",
                    "Новый клиент", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Разбиваем введённый текст на фамилию и имя
                    string[] parts = enteredText.Trim().Split(' ');
                    string surname = parts.Length > 0 ? parts[0] : "";
                    string name = parts.Length > 1 ? parts[1] : "";

                    // Открываем окно добавления клиента
                    var addClientWindow = new AddClientWindow();
                    if (addClientWindow.ShowDialog() == true)
                    {
                        // Перезагружаем список клиентов и выбираем нового
                        LoadClients();

                        // Ищем созданного клиента
                        foreach (var client in ClientComboBox.ItemsSource as IEnumerable<dynamic>)
                        {
                            if (client.DisplayName == enteredText ||
                                (client.DisplayName.Contains(surname) && client.DisplayName.Contains(name)))
                            {
                                ClientComboBox.SelectedItem = client;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    ClientComboBox.Text = "";
                    ClientComboBox.SelectedItem = null;
                }
            }
        }

        private void LoadClients()
        {
            var clients = new List<dynamic>();
            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT client_id, client_surname, client_name FROM clients ORDER BY client_surname";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string surname = reader.GetString(1);
                            string name = reader.GetString(2);
                            clients.Add(new { ClientId = id, DisplayName = $"{surname} {name}" });
                        }
                    }
                }
                ClientComboBox.ItemsSource = clients;
                ClientComboBox.SelectedValuePath = "ClientId";
                ClientComboBox.DisplayMemberPath = "DisplayName";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}");
            }
        }

        private void LoadServices()
        {
            var services = new List<dynamic>();
            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT DISTINCT s.service_id, s.service_name, ph.price_history_price 
                                  FROM services s
                                  JOIN price_history ph ON s.service_id = ph.service_id
                                  ORDER BY s.service_name";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string name = reader.GetString(1);
                            decimal price = reader.GetDecimal(2);
                            services.Add(new { ServiceId = id, ServiceName = name, Price = price });
                            _servicePrices[id] = price;
                        }
                    }
                }
                ServiceComboBox.ItemsSource = services;
                ServiceComboBox.SelectedValuePath = "ServiceId";
                ServiceComboBox.DisplayMemberPath = "ServiceName";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки услуг: {ex.Message}");
            }
        }

        private void LoadMasters()
        {
            var masters = new List<dynamic>();
            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT master_id, master_surname, master_name FROM masters ORDER BY master_surname";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string surname = reader.GetString(1);
                            string name = reader.GetString(2);
                            masters.Add(new { MasterId = id, DisplayName = $"{surname} {name}" });
                        }
                    }
                }
                MasterComboBox.ItemsSource = masters;
                MasterComboBox.SelectedValuePath = "MasterId";
                MasterComboBox.DisplayMemberPath = "DisplayName";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мастеров: {ex.Message}");
            }
        }

        private void ServiceComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ServiceComboBox.SelectedValue != null)
            {
                int serviceId = (int)ServiceComboBox.SelectedValue;
                if (_servicePrices.ContainsKey(serviceId))
                {
                    PriceTextBox.Text = _servicePrices[serviceId].ToString("F2");
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (ClientComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите клиента!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ServiceComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите услугу!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MasterComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите мастера!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TimeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите время!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedTime = (TimeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (!TimeSpan.TryParse(selectedTime, out TimeSpan time))
            {
                MessageBox.Show("Выберите корректное время!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int clientId = (int)ClientComboBox.SelectedValue;
            int serviceId = (int)ServiceComboBox.SelectedValue;
            int masterId = (int)MasterComboBox.SelectedValue;
            DateTime date = DatePicker.SelectedDate.Value;
            decimal price = decimal.Parse(PriceTextBox.Text);
            bool paymentStatus = PaymentCheckBox.IsChecked ?? false;
            string notes = NotesTextBox.Text.Trim();

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    // Получаем актуальную цену из price_history
                    string getPriceSql = @"SELECT price_history_id, price_history_price 
                                          FROM price_history 
                                          WHERE service_id = @serviceId 
                                          AND price_history_application_date <= @date 
                                          ORDER BY price_history_application_date DESC 
                                          LIMIT 1";
                    int priceHistoryId;
                    using (var cmd = new NpgsqlCommand(getPriceSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@serviceId", serviceId);
                        cmd.Parameters.AddWithValue("@date", date);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                priceHistoryId = reader.GetInt32(0);
                                price = reader.GetDecimal(1);
                            }
                            else
                            {
                                MessageBox.Show("Для выбранной услуги не установлена цена!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }

                    // Создаём запись
                    string insertSql = @"INSERT INTO appointments 
                                        (client_id, master_id, price_history_id, appointment_date, appointment_time, 
                                         appointment_status, payment_status, service_price, notes)
                                        VALUES (@clientId, @masterId, @priceHistoryId, @date, @time, 
                                                'Запланирована', @paymentStatus, @price, @notes)";

                    using (var cmd = new NpgsqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@clientId", clientId);
                        cmd.Parameters.AddWithValue("@masterId", masterId);
                        cmd.Parameters.AddWithValue("@priceHistoryId", priceHistoryId);
                        cmd.Parameters.AddWithValue("@date", date);
                        cmd.Parameters.AddWithValue("@time", time);
                        cmd.Parameters.AddWithValue("@paymentStatus", paymentStatus);
                        cmd.Parameters.AddWithValue("@price", price);
                        cmd.Parameters.AddWithValue("@notes", string.IsNullOrEmpty(notes) ? DBNull.Value : (object)notes);

                        cmd.ExecuteNonQuery();
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении записи:\n{ex.Message}",
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