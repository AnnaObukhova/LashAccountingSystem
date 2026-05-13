using System;
using System.Collections.Generic;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;
using System.Windows.Controls;

namespace LashAccountingSystem
{
    public partial class ScheduleWindow : Window
    {
        private DateTime _currentFilterDate;
        private DateTime _lastClickTime = DateTime.MinValue;
        private Appointment _lastClickedAppointment = null;
        private DateTime _lastClientClickTime = DateTime.MinValue;
        private Appointment _lastClickedClient = null;
        private DateTime _lastStartTimeClickTime = DateTime.MinValue;
        private Appointment _lastClickedStartTime = null;

        public ScheduleWindow()
        {
            InitializeComponent();
            _currentFilterDate = DateTime.Today;
            FilterDatePicker.SelectedDate = _currentFilterDate;
            LoadAppointments();
        }

        private void ScheduleWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAppointments();
        }

        private void LoadAppointments()
        {
            List<Appointment> appointments = new List<Appointment>();

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT 
                            a.appointment_id,
                            a.client_id,
                            c.client_surname || ' ' || c.client_name as client_name,
                            a.master_id,
                            m.master_surname || ' ' || m.master_name as master_name,
                            a.price_history_id,
                            s.service_name,
                            a.service_price,
                            a.appointment_date,
                            a.appointment_time,
                            a.appointment_status,
                            a.payment_status,
                            a.payment_method,
                            a.notes,
                            s.service_duration
                        FROM appointments a
                        JOIN clients c ON a.client_id = c.client_id
                        JOIN masters m ON a.master_id = m.master_id
                        JOIN price_history ph ON a.price_history_id = ph.price_history_id
                        JOIN services s ON ph.service_id = s.service_id
                        WHERE a.appointment_date = @date
                        ORDER BY a.appointment_time";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@date", _currentFilterDate);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                TimeSpan startTime = reader.GetTimeSpan(9);
                                int durationMinutes = reader.GetInt32(14); // service_duration
                                TimeSpan endTime = startTime.Add(TimeSpan.FromMinutes(durationMinutes));

                                appointments.Add(new Appointment
                                {
                                    AppointmentId = reader.GetInt32(0),
                                    ClientId = reader.GetInt32(1),
                                    ClientName = reader.GetString(2),
                                    MasterId = reader.GetInt32(3),
                                    MasterName = reader.GetString(4),
                                    PriceHistoryId = reader.GetInt32(5),
                                    ServiceName = reader.GetString(6),
                                    ServicePrice = reader.GetDecimal(7),
                                    AppointmentDate = reader.GetDateTime(8),
                                    AppointmentTime = startTime,
                                    EndTime = endTime,  // <-- ДОБАВИТЬ
                                    AppointmentStatus = reader.GetString(10),
                                    PaymentStatus = reader.GetBoolean(11),
                                    PaymentMethod = reader.IsDBNull(12) ? null : reader.GetString(12),
                                    Notes = reader.IsDBNull(13) ? null : reader.GetString(13)
                                });
                            }
                        }
                    }
                }

                MyDataGrid.ItemsSource = appointments;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки записей: {ex.Message}", "Ошибка");
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilterDatePicker.SelectedDate.HasValue)
            {
                _currentFilterDate = FilterDatePicker.SelectedDate.Value;
                LoadAppointments();
            }
        }

        private void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            _currentFilterDate = DateTime.Today;
            FilterDatePicker.SelectedDate = _currentFilterDate;
            LoadAppointments();
        }

        private void FilterDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilterDatePicker.SelectedDate.HasValue)
            {
                _currentFilterDate = FilterDatePicker.SelectedDate.Value;
                LoadAppointments();
            }
        }

        private void FilterDatePicker_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (FilterDatePicker.SelectedDate.HasValue)
            {
                _currentFilterDate = FilterDatePicker.SelectedDate.Value;
                LoadAppointments();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddAppointmentWindow(_currentFilterDate);
            if (addWindow.ShowDialog() == true)
            {
                LoadAppointments();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (MyDataGrid.SelectedItem is Appointment selectedAppointment)
            {
                var editWindow = new EditAppointmentWindow(selectedAppointment);
                if (editWindow.ShowDialog() == true)
                {
                    LoadAppointments();
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (MyDataGrid.SelectedItem is Appointment selectedAppointment)
            {
                var result = MessageBox.Show($"Удалить запись?", "Подтверждение", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    using (var conn = DbConnection.GetConnection())
                    {
                        conn.Open();
                        string sql = "DELETE FROM appointments WHERE appointment_id = @id";
                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", selectedAppointment.AppointmentId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LoadAppointments();
                }
            }
        }

        private void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (MyDataGrid.SelectedItem is Appointment selectedAppointment)
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = "UPDATE appointments SET appointment_status = 'Выполнена' WHERE appointment_id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedAppointment.AppointmentId);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadAppointments();
            }
        }

        private void ClientsButton_Click(object sender, RoutedEventArgs e)
        {
            var clientsWindow = new MainWindow();
            clientsWindow.ShowDialog();
        }

        private void ServicesButton_Click(object sender, RoutedEventArgs e)
        {
            var servicesWindow = new ServicesWindow();
            servicesWindow.ShowDialog();
        }

        private void MaterialsButton_Click(object sender, RoutedEventArgs e)
        {
            var materialsWindow = new MaterialsWindow();
            materialsWindow.ShowDialog();
        }

        // Двойной клик по времени начала - открывает редактирование записи
        private void StartTime_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var textBlock = sender as TextBlock;
            if (textBlock != null && textBlock.DataContext is Appointment selectedAppointment)
            {
                if (_lastClickedStartTime == selectedAppointment &&
                    (DateTime.Now - _lastStartTimeClickTime).TotalMilliseconds < 500)
                {
                    var editWindow = new EditAppointmentWindow(selectedAppointment);
                    if (editWindow.ShowDialog() == true)
                    {
                        LoadAppointments();
                    }
                    _lastStartTimeClickTime = DateTime.MinValue;
                    _lastClickedStartTime = null;
                }
                else
                {
                    _lastStartTimeClickTime = DateTime.Now;
                    _lastClickedStartTime = selectedAppointment;
                }
            }
        }

        private void ClientName_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var textBlock = sender as TextBlock;
            if (textBlock != null && textBlock.DataContext is Appointment selectedAppointment)
            {
                // Проверяем, был ли двойной клик (разница между кликами меньше 500 мс)
                if (_lastClickedClient == selectedAppointment &&
                    (DateTime.Now - _lastClientClickTime).TotalMilliseconds < 500)
                {
                    // Двойной клик - открываем карточку клиента
                    var cardWindow = new ClientCardWindow(selectedAppointment.ClientId, selectedAppointment.ClientName);
                    cardWindow.ShowDialog();

                    // Сбрасываем
                    _lastClientClickTime = DateTime.MinValue;
                    _lastClickedClient = null;
                }
                else
                {
                    // Первый клик - запоминаем
                    _lastClientClickTime = DateTime.Now;
                    _lastClickedClient = selectedAppointment;
                }
            }
        }


        private void ServiceName_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var textBlock = sender as TextBlock;
            if (textBlock != null && textBlock.DataContext is Appointment selectedAppointment)
            {
                // Проверяем, был ли двойной клик (разница между кликами меньше 500 мс)
                if (_lastClickedAppointment == selectedAppointment &&
                    (DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
                {
                    // Двойной клик - открываем редактирование
                    var editWindow = new EditAppointmentWindow(selectedAppointment);
                    if (editWindow.ShowDialog() == true)
                    {
                        LoadAppointments();
                    }

                    // Сбрасываем
                    _lastClickTime = DateTime.MinValue;
                    _lastClickedAppointment = null;
                }
                else
                {
                    // Первый клик - запоминаем
                    _lastClickTime = DateTime.Now;
                    _lastClickedAppointment = selectedAppointment;
                }
            }
        }

        // Кнопка "Предыдущий день" (вчера)
        private void PrevDayButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilterDatePicker.SelectedDate.HasValue)
            {
                _currentFilterDate = FilterDatePicker.SelectedDate.Value.AddDays(-1);
                FilterDatePicker.SelectedDate = _currentFilterDate;
                LoadAppointments();
            }
        }

        // Кнопка "Следующий день" (завтра)
        private void NextDayButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilterDatePicker.SelectedDate.HasValue)
            {
                _currentFilterDate = FilterDatePicker.SelectedDate.Value.AddDays(1);
                FilterDatePicker.SelectedDate = _currentFilterDate;
                LoadAppointments();
            }
        }

        private void MaterialConsumptionButton_Click(object sender, RoutedEventArgs e)
        {
            var consumptionWindow = new MaterialConsumptionWindow();
            consumptionWindow.ShowDialog();
        }

        private void PriceHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var priceHistoryWindow = new PriceHistoryWindow();
            priceHistoryWindow.ShowDialog();
        }        
    }
}