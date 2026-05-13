using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class MaterialConsumptionWindow : Window
    {
        private int _selectedAppointmentId = -1;
        private List<dynamic> _appointments = new List<dynamic>();

        public MaterialConsumptionWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            LoadAppointments();
            LoadMaterials();
        }

        private void LoadAppointments()
        {
            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT 
                            a.appointment_id,
                            c.client_surname || ' ' || c.client_name as client_name,
                            s.service_name,
                            a.appointment_date,
                            a.appointment_time,
                            a.appointment_status
                        FROM appointments a
                        JOIN clients c ON a.client_id = c.client_id
                        JOIN price_history ph ON a.price_history_id = ph.price_history_id
                        JOIN services s ON ph.service_id = s.service_id
                        ORDER BY a.appointment_date DESC, a.appointment_time";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        _appointments.Clear();
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string clientName = reader.GetString(1);
                            string serviceName = reader.GetString(2);
                            DateTime date = reader.GetDateTime(3);
                            TimeSpan time = reader.GetTimeSpan(4);
                            string status = reader.GetString(5);

                            _appointments.Add(new
                            {
                                AppointmentId = id,
                                DisplayName = $"{date:dd.MM.yyyy} {time:hh\\:mm} - {clientName} - {serviceName} [{status}]",
                                ClientName = clientName,
                                ServiceName = serviceName,
                                Date = date,
                                Time = time,
                                Status = status
                            });
                        }
                    }
                }
                AppointmentComboBox.ItemsSource = _appointments;
                AppointmentComboBox.SelectedValuePath = "AppointmentId";
                AppointmentComboBox.DisplayMemberPath = "DisplayName";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки записей: {ex.Message}", "Ошибка");
            }
        }

        private void LoadMaterials()
        {
            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT material_id, material_name, material_stock FROM materials WHERE material_stock > 0 ORDER BY material_name";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var materials = new List<dynamic>();
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string name = reader.GetString(1);
                            decimal stock = reader.GetDecimal(2);
                            materials.Add(new { MaterialId = id, DisplayName = $"{name} (остаток: {stock})" });
                        }
                        MaterialComboBox.ItemsSource = materials;
                        MaterialComboBox.SelectedValuePath = "MaterialId";
                        MaterialComboBox.DisplayMemberPath = "DisplayName";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки материалов: {ex.Message}", "Ошибка");
            }
        }

        private void LoadConsumptionHistory()
        {
            if (_selectedAppointmentId <= 0) return;

            var consumption = new List<MaterialConsumption>();

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT 
                            mc.consumption_id,
                            mc.appointment_id,
                            m.material_name,
                            mc.material_consumption_amount,
                            a.appointment_date,
                            a.appointment_time
                        FROM materials_consumption mc
                        JOIN materials m ON mc.material_id = m.material_id
                        JOIN appointments a ON mc.appointment_id = a.appointment_id
                        WHERE mc.appointment_id = @appointmentId
                        ORDER BY mc.consumption_id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@appointmentId", _selectedAppointmentId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                consumption.Add(new MaterialConsumption
                                {
                                    ConsumptionId = reader.GetInt32(0),
                                    AppointmentId = reader.GetInt32(1),
                                    MaterialName = reader.GetString(2),
                                    MaterialConsumptionAmount = reader.GetDecimal(3),
                                    AppointmentDate = reader.GetDateTime(4),
                                    AppointmentTime = reader.GetTimeSpan(5)
                                });
                            }
                        }
                    }
                }
                ConsumptionDataGrid.ItemsSource = consumption;

                // Подсчитываем общий расход
                decimal total = consumption.Sum(c => c.MaterialConsumptionAmount);
                TotalConsumptionText.Text = $"{total:F2} ед.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки расхода материалов: {ex.Message}", "Ошибка");
            }
        }

        private void UpdateAppointmentInfo()
        {
            var selected = _appointments.FirstOrDefault(a => a.AppointmentId == _selectedAppointmentId);
            if (selected != null)
            {
                ClientNameText.Text = selected.ClientName;
                ServiceNameText.Text = selected.ServiceName;
                DateTimeText.Text = $"{selected.Date:dd.MM.yyyy} {selected.Time:hh\\:mm}";
                StatusText.Text = selected.Status;
            }
            else
            {
                ClientNameText.Text = "";
                ServiceNameText.Text = "";
                DateTimeText.Text = "";
                StatusText.Text = "";
            }
        }

        private void AppointmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppointmentComboBox.SelectedValue != null)
            {
                _selectedAppointmentId = (int)AppointmentComboBox.SelectedValue;
                UpdateAppointmentInfo();
                LoadConsumptionHistory();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAppointments();
            LoadMaterials();
            if (_selectedAppointmentId > 0)
            {
                LoadConsumptionHistory();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAppointmentId <= 0)
            {
                MessageBox.Show("Сначала выберите запись!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MaterialComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите материал!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(AmountTextBox.Text))
            {
                MessageBox.Show("Введите количество материала!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(AmountTextBox.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Введите корректное количество (положительное число)!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int materialId = (int)MaterialComboBox.SelectedValue;

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    // Проверяем остаток материала
                    string checkStockSql = "SELECT material_stock FROM materials WHERE material_id = @materialId";
                    decimal currentStock;
                    using (var cmd = new NpgsqlCommand(checkStockSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@materialId", materialId);
                        currentStock = (decimal)cmd.ExecuteScalar();
                    }

                    if (currentStock < amount)
                    {
                        MessageBox.Show($"Недостаточно материала! Доступно: {currentStock} ед.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Добавляем расход
                    string insertSql = @"INSERT INTO materials_consumption (appointment_id, material_id, material_consumption_amount) 
                                        VALUES (@appointmentId, @materialId, @amount)";
                    using (var cmd = new NpgsqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@appointmentId", _selectedAppointmentId);
                        cmd.Parameters.AddWithValue("@materialId", materialId);
                        cmd.Parameters.AddWithValue("@amount", amount);
                        cmd.ExecuteNonQuery();
                    }

                    // Обновляем остаток материала
                    string updateStockSql = "UPDATE materials SET material_stock = material_stock - @amount WHERE material_id = @materialId";
                    using (var cmd = new NpgsqlCommand(updateStockSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@amount", amount);
                        cmd.Parameters.AddWithValue("@materialId", materialId);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Очищаем поле ввода
                AmountTextBox.Text = "";

                // Обновляем данные
                LoadMaterials();
                LoadConsumptionHistory();

                MessageBox.Show("Расход материала добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении расхода: {ex.Message}", "Ошибка");
            }
        }

        private void DeleteConsumptionButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                int consumptionId = (int)button.Tag;

                var result = MessageBox.Show("Удалить запись о расходе материала?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var conn = DbConnection.GetConnection())
                        {
                            conn.Open();

                            // Сначала получаем информацию о расходе (материал и количество)
                            string getSql = "SELECT material_id, material_consumption_amount FROM materials_consumption WHERE consumption_id = @id";
                            int materialId = 0;
                            decimal amount = 0;
                            using (var cmd = new NpgsqlCommand(getSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", consumptionId);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        materialId = reader.GetInt32(0);
                                        amount = reader.GetDecimal(1);
                                    }
                                }
                            }

                            // Удаляем запись о расходе
                            string deleteSql = "DELETE FROM materials_consumption WHERE consumption_id = @id";
                            using (var cmd = new NpgsqlCommand(deleteSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", consumptionId);
                                cmd.ExecuteNonQuery();
                            }

                            // Возвращаем материал на склад
                            string updateStockSql = "UPDATE materials SET material_stock = material_stock + @amount WHERE material_id = @materialId";
                            using (var cmd = new NpgsqlCommand(updateStockSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@amount", amount);
                                cmd.Parameters.AddWithValue("@materialId", materialId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        LoadMaterials();
                        LoadConsumptionHistory();

                        MessageBox.Show("Запись о расходе удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка");
                    }
                }
            }
        }
    }
}