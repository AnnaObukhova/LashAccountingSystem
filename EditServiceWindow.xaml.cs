using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;
using System.Windows.Controls;

namespace LashAccountingSystem
{
    public partial class EditServiceWindow : Window
    {
        private Service _service;
        private List<ServiceMaterial> _materials = new List<ServiceMaterial>();
        private int _nextTempId = 1;

        public EditServiceWindow(Service service)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            _service = service;
            LoadServiceData();
            LoadMaterials();
        }

        private void LoadServiceData()
        {
            IdTextBox.Text = _service.ServiceId.ToString();
            NameTextBox.Text = _service.ServiceName;
            DurationTextBox.Text = _service.ServiceDuration.ToString();
            DescriptionTextBox.Text = _service.ServiceDescription ?? "";
        }

        private void LoadMaterials()
        {
            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT sm.id, m.material_id, m.material_name, sm.quantity_required 
                                  FROM service_materials sm
                                  JOIN materials m ON sm.material_id = m.material_id
                                  WHERE sm.service_id = @serviceId
                                  ORDER BY m.material_name";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@serviceId", _service.ServiceId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _materials.Add(new ServiceMaterial
                                {
                                    Id = reader.GetInt32(0),
                                    TempId = _nextTempId++,
                                    MaterialId = reader.GetInt32(1),
                                    MaterialName = reader.GetString(2),
                                    QuantityRequired = reader.GetDecimal(3)
                                });
                            }
                        }
                    }
                }

                RefreshMaterialsGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки материалов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshMaterialsGrid()
        {
            MaterialsDataGrid.ItemsSource = null;
            MaterialsDataGrid.ItemsSource = _materials;
        }

        private void AddMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddServiceMaterialTempWindow(_materials, _nextTempId);
            if (addWindow.ShowDialog() == true && addWindow.SelectedMaterial != null)
            {
                _materials.Add(addWindow.SelectedMaterial);
                _nextTempId++;
                RefreshMaterialsGrid();
            }
        }

        private void DeleteMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                int tempId = (int)button.Tag;
                var material = _materials.FirstOrDefault(m => m.TempId == tempId);
                if (material != null)
                {
                    _materials.Remove(material);
                    RefreshMaterialsGrid();
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите название услуги!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (!int.TryParse(DurationTextBox.Text, out int duration) || duration <= 0)
            {
                MessageBox.Show("Введите корректную длительность (положительное число)!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DurationTextBox.Focus();
                return;
            }

            // Проверка наличия материалов
            if (_materials == null || _materials.Count == 0)
            {
                MessageBox.Show("❌ Добавьте хотя бы один материал для услуги!\n\n" +
                    "Услуга не может существовать без необходимых материалов.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка количества материалов
            foreach (var material in _materials)
            {
                if (material.QuantityRequired <= 0)
                {
                    MessageBox.Show($"❌ У материала '{material.MaterialName}' количество должно быть больше 0!",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    // Начинаем транзакцию
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Обновляем услуги
                            string updateServiceSql = @"UPDATE services SET 
                                                          service_name = @name,
                                                          service_description = @description,
                                                          service_duration = @duration
                                                          WHERE service_id = @id";

                            using (var cmd = new NpgsqlCommand(updateServiceSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", _service.ServiceId);
                                cmd.Parameters.AddWithValue("@name", NameTextBox.Text.Trim());
                                cmd.Parameters.AddWithValue("@description",
                                    string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? DBNull.Value : (object)DescriptionTextBox.Text.Trim());
                                cmd.Parameters.AddWithValue("@duration", duration);
                                cmd.ExecuteNonQuery();
                            }

                            // 2. Удаляем старые связи с материалами
                            string deleteMaterialsSql = "DELETE FROM service_materials WHERE service_id = @serviceId";
                            using (var cmd = new NpgsqlCommand(deleteMaterialsSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@serviceId", _service.ServiceId);
                                cmd.ExecuteNonQuery();
                            }

                            // 3. Добавляем новые связи с материалами
                            foreach (var material in _materials)
                            {
                                string insertMaterialSql = @"INSERT INTO service_materials (service_id, material_id, quantity_required) 
                                                             VALUES (@serviceId, @materialId, @quantity)";
                                using (var cmd = new NpgsqlCommand(insertMaterialSql, conn))
                                {
                                    cmd.Parameters.AddWithValue("@serviceId", _service.ServiceId);
                                    cmd.Parameters.AddWithValue("@materialId", material.MaterialId);
                                    cmd.Parameters.AddWithValue("@quantity", material.QuantityRequired);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении услуги:\n{ex.Message}",
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