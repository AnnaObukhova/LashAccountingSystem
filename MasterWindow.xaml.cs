using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class MastersWindow : Window
    {
        private List<Master> _allMasters;
        private List<Master> _filteredMasters;

        public MastersWindow()
        {
            InitializeComponent();
            LoadMasters();
        }

        private void LoadMasters(string searchText = "")
        {
            try
            {
                _allMasters = new List<Master>();

                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT master_id, master_surname, master_name, master_patronymic, " +
                                 "master_phone_number, master_specialization, master_contract_number " +
                                 "FROM masters ORDER BY master_surname, master_name";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _allMasters.Add(new Master
                            {
                                MasterId = reader.GetInt32(0),
                                MasterSurname = reader.GetString(1),
                                MasterName = reader.GetString(2),
                                MasterPatronymic = reader.IsDBNull(3) ? null : reader.GetString(3),
                                MasterPhoneNumber = reader.GetString(4),
                                MasterSpecialization = reader.IsDBNull(5) ? null : reader.GetString(5),
                                MasterContractNumber = reader.GetString(6)
                            });
                        }
                    }
                }

                ApplyFilter(searchText);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мастеров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredMasters = _allMasters;
            }
            else
            {
                string lowerSearch = searchText.ToLower();
                _filteredMasters = _allMasters.Where(m =>
                    (m.MasterSurname?.ToLower().Contains(lowerSearch) ?? false) ||
                    (m.MasterName?.ToLower().Contains(lowerSearch) ?? false) ||
                    (m.MasterPatronymic?.ToLower().Contains(lowerSearch) ?? false) ||
                    (m.MasterPhoneNumber?.Contains(lowerSearch) ?? false) ||
                    (m.MasterSpecialization?.ToLower().Contains(lowerSearch) ?? false) ||
                    (m.MasterContractNumber?.Contains(lowerSearch) ?? false)
                ).ToList();
            }

            MastersDataGrid.ItemsSource = _filteredMasters;
            SearchResultCount.Text = $"Найдено: {_filteredMasters.Count} из {_allMasters.Count}";
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
            var addWindow = new AddEditMasterWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadMasters(SearchTextBox?.Text ?? "");
                MessageBox.Show("Мастер успешно добавлен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (MastersDataGrid.SelectedItem is Master selectedMaster)
            {
                var editWindow = new AddEditMasterWindow(selectedMaster);
                if (editWindow.ShowDialog() == true)
                {
                    LoadMasters(SearchTextBox?.Text ?? "");
                    MessageBox.Show("Данные мастера успешно обновлены!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Выберите мастера для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (MastersDataGrid.SelectedItem is Master selectedMaster)
            {
                // Проверка, есть ли у мастера записи
                bool hasAppointments = CheckMasterHasAppointments(selectedMaster.MasterId);

                if (hasAppointments)
                {
                    MessageBox.Show($"Невозможно удалить мастера \"{selectedMaster.MasterSurname} {selectedMaster.MasterName}\",\n" +
                        "так как у него есть записи в расписании.\n\n" +
                        "Сначала удалите или переназначьте записи мастера.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Удалить мастера {selectedMaster.MasterSurname} {selectedMaster.MasterName}?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var conn = DbConnection.GetConnection())
                        {
                            conn.Open();
                            string sql = "DELETE FROM masters WHERE master_id = @id";
                            using (var cmd = new NpgsqlCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", selectedMaster.MasterId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        LoadMasters(SearchTextBox?.Text ?? "");
                        MessageBox.Show("Мастер удалён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Выберите мастера для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool CheckMasterHasAppointments(int masterId)
        {
            using (var conn = DbConnection.GetConnection())
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM appointments WHERE master_id = @masterId";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@masterId", masterId);
                    long count = (long)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        private void MastersDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MastersDataGrid.SelectedItem is Master selectedMaster)
            {
                var editWindow = new AddEditMasterWindow(selectedMaster);
                if (editWindow.ShowDialog() == true)
                {
                    LoadMasters(SearchTextBox?.Text ?? "");
                }
            }
        }
    }
}