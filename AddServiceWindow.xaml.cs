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
    public partial class AddServiceWindow : Window
    {
        private List<ServiceMaterial> _tempMaterials = new List<ServiceMaterial>();
        private int _nextTempId = 1;

        public AddServiceWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            LoadMaterialsComboBox();
        }

        private void LoadMaterialsComboBox()
        {
            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT material_id, material_name, material_stock FROM materials ORDER BY material_name";
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
                        // Сохраняем список материалов для выбора
                        App.Current.Resources["MaterialsList"] = materials;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки материалов: {ex.Message}");
            }
        }

        private void AddMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddServiceMaterialTempWindow(_tempMaterials, _nextTempId);
            if (addWindow.ShowDialog() == true && addWindow.SelectedMaterial != null)
            {
                _tempMaterials.Add(addWindow.SelectedMaterial);
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
                var material = _tempMaterials.FirstOrDefault(m => m.TempId == tempId);
                if (material != null)
                {
                    _tempMaterials.Remove(material);
                    RefreshMaterialsGrid();
                }
            }
        }

        private void RefreshMaterialsGrid()
        {
            MaterialsDataGrid.ItemsSource = null;
            MaterialsDataGrid.ItemsSource = _tempMaterials;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите название услуги!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (!int.TryParse(DurationTextBox.Text, out int duration) || duration <= 0)
            {
                MessageBox.Show("Введите корректную длительность (положительное число)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                DurationTextBox.Focus();
                return;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price <= 0)
            {
                MessageBox.Show("Введите корректную цену (положительное число)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PriceTextBox.Focus();
                return;
            }

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    // Вставляем услугу
                    string insertServiceSql = @"INSERT INTO services (service_name, service_description, service_duration) 
                                                VALUES (@name, @description, @duration) RETURNING service_id";

                    int serviceId;
                    using (var cmd = new NpgsqlCommand(insertServiceSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", NameTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@description", string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? DBNull.Value : (object)DescriptionTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@duration", duration);
                        serviceId = (int)cmd.ExecuteScalar();
                    }

                    // Добавляем цену в историю цен
                    string insertPriceSql = @"INSERT INTO price_history (service_id, price_history_price, price_history_application_date) 
                                              VALUES (@serviceId, @price, CURRENT_DATE)";
                    using (var cmd = new NpgsqlCommand(insertPriceSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@serviceId", serviceId);
                        cmd.Parameters.AddWithValue("@price", price);
                        cmd.ExecuteNonQuery();
                    }

                    // Добавляем материалы для услуги
                    foreach (var material in _tempMaterials)
                    {
                        string insertMaterialSql = @"INSERT INTO service_materials (service_id, material_id, quantity_required) 
                                                     VALUES (@serviceId, @materialId, @quantity)";
                        using (var cmd = new NpgsqlCommand(insertMaterialSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@serviceId", serviceId);
                            cmd.Parameters.AddWithValue("@materialId", material.MaterialId);
                            cmd.Parameters.AddWithValue("@quantity", material.QuantityRequired);
                            cmd.ExecuteNonQuery();
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