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

                    // ============================================
                    // ШАГ 1: ПРОВЕРКА НАЛИЧИЯ МАТЕРИАЛОВ
                    // ============================================
                    string checkMaterialsSql = @"
                SELECT m.material_name, sm.quantity_required, m.material_stock
                FROM service_materials sm
                JOIN materials m ON sm.material_id = m.material_id
                WHERE sm.service_id = @serviceId";

                    List<string> missingMaterials = new List<string>();

                    using (var cmdCheck = new NpgsqlCommand(checkMaterialsSql, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@serviceId", serviceId);
                        using (var reader = cmdCheck.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string materialName = reader.GetString(0);
                                decimal required = reader.GetDecimal(1);
                                decimal stock = reader.GetDecimal(2);

                                if (stock < required)
                                {
                                    missingMaterials.Add($"{materialName} (нужно {required}, есть {stock})");
                                }
                            }
                        }
                    }

                    if (missingMaterials.Count > 0)
                    {
                        MessageBox.Show($"Невозможно создать запись. Недостаточно материалов:\n{string.Join("\n", missingMaterials)}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // ============================================
                    // ШАГ 2: ПОЛУЧЕНИЕ АКТУАЛЬНОЙ ЦЕНЫ
                    // ============================================
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
                                MessageBox.Show("Для выбранной услуги не установлена цена!", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }

                    // ============================================
                    // ШАГ 3: СОЗДАНИЕ ЗАПИСИ
                    // ============================================
                    string insertSql = @"INSERT INTO appointments 
                                (client_id, master_id, price_history_id, appointment_date, appointment_time, 
                                 appointment_status, payment_status, service_price, notes)
                                VALUES (@clientId, @masterId, @priceHistoryId, @date, @time, 
                                        'Запланирована', @paymentStatus, @price, @notes)
                                RETURNING appointment_id";

                    int newAppointmentId;
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

                        newAppointmentId = (int)cmd.ExecuteScalar();
                    }

                    // ============================================
                    // ШАГ 4: СПИСАНИЕ МАТЕРИАЛОВ
                    // ============================================
                    string getMaterialsSql = @"
                SELECT material_id, quantity_required
                FROM service_materials
                WHERE service_id = @serviceId";

                    using (var cmdGet = new NpgsqlCommand(getMaterialsSql, conn))
                    {
                        cmdGet.Parameters.AddWithValue("@serviceId", serviceId);
                        using (var reader = cmdGet.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int materialId = reader.GetInt32(0);
                                decimal required = reader.GetDecimal(1);

                                // Обновляем остаток материала
                                string updateStockSql = "UPDATE materials SET material_stock = material_stock - @qty WHERE material_id = @mid";
                                using (var cmdUpdate = new NpgsqlCommand(updateStockSql, conn))
                                {
                                    cmdUpdate.Parameters.AddWithValue("@qty", required);
                                    cmdUpdate.Parameters.AddWithValue("@mid", materialId);
                                    cmdUpdate.ExecuteNonQuery();
                                }

                                // Записываем расход в materials_consumption
                                string insertConsumptionSql = @"INSERT INTO materials_consumption 
                            (appointment_id, material_id, material_consumption_amount) 
                            VALUES (@appointmentId, @mid, @qty)";
                                using (var cmdInsert = new NpgsqlCommand(insertConsumptionSql, conn))
                                {
                                    cmdInsert.Parameters.AddWithValue("@appointmentId", newAppointmentId);
                                    cmdInsert.Parameters.AddWithValue("@mid", materialId);
                                    cmdInsert.Parameters.AddWithValue("@qty", required);
                                    cmdInsert.ExecuteNonQuery();
                                }
                            }
                        }
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