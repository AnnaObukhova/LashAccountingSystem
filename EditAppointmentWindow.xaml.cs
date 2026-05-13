using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class EditAppointmentWindow : Window
    {
        private Appointment _appointment;
        private Dictionary<int, decimal> _servicePrices = new Dictionary<int, decimal>();
        private Dictionary<int, int> _priceHistoryIds = new Dictionary<int, int>();

        public EditAppointmentWindow(Appointment appointment)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            _appointment = appointment;
            LoadClients();
            LoadServices();
            LoadMasters();
            LoadAppointmentData();

            // Добавляем обработчик для создания нового клиента
            ClientComboBox.LostFocus += ClientComboBox_LostFocus;
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
                    string sql = @"SELECT s.service_id, s.service_name, ph.price_history_price, ph.price_history_id 
                                  FROM services s
                                  JOIN price_history ph ON s.service_id = ph.service_id
                                  ORDER BY s.service_name, ph.price_history_application_date DESC";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string name = reader.GetString(1);
                            decimal price = reader.GetDecimal(2);
                            int priceId = reader.GetInt32(3);

                            if (!services.Exists(s => s.ServiceId == id))
                            {
                                services.Add(new { ServiceId = id, ServiceName = name, Price = price });
                            }
                            _servicePrices[id] = price;
                            _priceHistoryIds[id] = priceId;
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

        private void LoadAppointmentData()
        {
            IdTextBox.Text = _appointment.AppointmentId.ToString();

            // Выбираем клиента в ComboBox
            foreach (var item in ClientComboBox.ItemsSource as IEnumerable<dynamic>)
            {
                if (item.ClientId == _appointment.ClientId)
                {
                    ClientComboBox.SelectedItem = item;
                    break;
                }
            }

            // Выбираем услугу в ComboBox
            foreach (var item in ServiceComboBox.ItemsSource as IEnumerable<dynamic>)
            {
                if (item.ServiceName == _appointment.ServiceName)
                {
                    ServiceComboBox.SelectedItem = item;
                    PriceTextBox.Text = item.Price.ToString("F2");
                    break;
                }
            }

            // Выбираем мастера в ComboBox
            foreach (var item in MasterComboBox.ItemsSource as IEnumerable<dynamic>)
            {
                if (item.MasterId == _appointment.MasterId)
                {
                    MasterComboBox.SelectedItem = item;
                    break;
                }
            }

            DatePicker.SelectedDate = _appointment.AppointmentDate;

            // Выбираем время в ComboBox
            string timeString = _appointment.AppointmentTime.ToString(@"hh\:mm");
            foreach (ComboBoxItem item in TimeComboBox.Items)
            {
                if (item.Content.ToString() == timeString)
                {
                    TimeComboBox.SelectedItem = item;
                    break;
                }
            }

            // Выбираем статус
            foreach (ComboBoxItem item in StatusComboBox.Items)
            {
                if (item.Content.ToString() == _appointment.AppointmentStatus)
                {
                    StatusComboBox.SelectedItem = item;
                    break;
                }
            }

            PaymentCheckBox.IsChecked = _appointment.PaymentStatus;
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
                        // Перезагружаем список клиентов
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

        private void ServiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            string status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Запланирована";
            bool paymentStatus = PaymentCheckBox.IsChecked ?? false;
            decimal price = decimal.Parse(PriceTextBox.Text);

            int priceHistoryId = _priceHistoryIds.ContainsKey(serviceId) ? _priceHistoryIds[serviceId] : 1;

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    string sql = @"UPDATE appointments SET 
                                  client_id = @clientId,
                                  master_id = @masterId,
                                  price_history_id = @priceHistoryId,
                                  appointment_date = @date,
                                  appointment_time = @time,
                                  appointment_status = @status,
                                  payment_status = @paymentStatus,
                                  service_price = @price
                                  WHERE appointment_id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _appointment.AppointmentId);
                        cmd.Parameters.AddWithValue("@clientId", clientId);
                        cmd.Parameters.AddWithValue("@masterId", masterId);
                        cmd.Parameters.AddWithValue("@priceHistoryId", priceHistoryId);
                        cmd.Parameters.AddWithValue("@date", date);
                        cmd.Parameters.AddWithValue("@time", time);
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@paymentStatus", paymentStatus);
                        cmd.Parameters.AddWithValue("@price", price);

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