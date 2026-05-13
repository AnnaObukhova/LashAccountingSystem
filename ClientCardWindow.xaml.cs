using System;
using System.Collections.Generic;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;

namespace LashAccountingSystem
{
    public partial class ClientCardWindow : Window
    {
        private int _clientId;

        public ClientCardWindow(int clientId, string clientName)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            _clientId = clientId;
            Title = $"Карточка клиента - {clientName}";
            LoadClientInfo();
            LoadClientHistory();
        }

        private void LoadClientInfo()
        {
            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT client_surname, client_name, client_patronymic, 
                                          client_phone_number, client_email, client_health_features
                                  FROM clients WHERE client_id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _clientId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string surname = reader.GetString(0);
                                string name = reader.GetString(1);
                                string patronymic = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                string phone = reader.GetString(3);
                                string email = reader.IsDBNull(4) ? "не указан" : reader.GetString(4);
                                string notes = reader.IsDBNull(5) ? "нет" : reader.GetString(5);

                                FullNameTextBlock.Text = $"{surname} {name} {patronymic}".Trim();
                                PhoneTextBlock.Text = phone;
                                EmailTextBlock.Text = email;
                                NotesTextBlock.Text = notes;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки информации о клиенте: {ex.Message}");
            }
        }

        private void LoadClientHistory()
        {
            var history = new List<dynamic>();

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT 
                                        a.appointment_date,
                                        a.appointment_time,
                                        s.service_name,
                                        m.master_surname || ' ' || m.master_name as master_name,
                                        a.service_price,
                                        a.appointment_status,
                                        a.payment_status
                                  FROM appointments a
                                  JOIN price_history ph ON a.price_history_id = ph.price_history_id
                                  JOIN services s ON ph.service_id = s.service_id
                                  JOIN masters m ON a.master_id = m.master_id
                                  WHERE a.client_id = @clientId
                                  ORDER BY a.appointment_date DESC, a.appointment_time DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@clientId", _clientId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            int totalVisits = 0;
                            decimal totalAmount = 0;

                            while (reader.Read())
                            {
                                DateTime date = reader.GetDateTime(0);
                                TimeSpan time = reader.GetTimeSpan(1);
                                string serviceName = reader.GetString(2);
                                string masterName = reader.GetString(3);
                                decimal price = reader.GetDecimal(4);
                                string status = reader.GetString(5);
                                bool paymentStatus = reader.GetBoolean(6);

                                history.Add(new
                                {
                                    AppointmentDate = date,
                                    AppointmentTime = time,
                                    ServiceName = serviceName,
                                    MasterName = masterName,
                                    ServicePrice = price,
                                    AppointmentStatus = status,
                                    PaymentStatus = paymentStatus
                                });

                                if (status == "Выполнена")
                                {
                                    totalVisits++;
                                    totalAmount += price;
                                }
                            }

                            TotalVisitsTextBlock.Text = totalVisits.ToString();
                            TotalAmountTextBlock.Text = $"{totalAmount:F2} ₽";
                        }
                    }
                }

                HistoryDataGrid.ItemsSource = history;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории записей: {ex.Message}");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // Сначала нужно получить полную информацию о клиенте
            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT client_id, client_surname, client_name, client_patronymic, " +
                                 "client_phone_number, client_email, client_health_features " +
                                 "FROM clients WHERE client_id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _clientId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var client = new Models.Client
                                {
                                    ClientId = reader.GetInt32(0),
                                    ClientSurname = reader.GetString(1),
                                    ClientName = reader.GetString(2),
                                    ClientPatronymic = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    ClientPhoneNumber = reader.GetString(4),
                                    ClientEmail = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    ClientHealthFeatures = reader.IsDBNull(6) ? null : reader.GetString(6)
                                };

                                var editWindow = new EditClientWindow(client);
                                if (editWindow.ShowDialog() == true)
                                {
                                    // Обновляем данные в карточке после редактирования
                                    LoadClientInfo();
                                    LoadClientHistory();
                                    MessageBox.Show("Данные клиента обновлены!", "Успех",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}