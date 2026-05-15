using System;
using System.Collections.Generic;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class MaterialIncomingWindow : Window
    {
        private List<Material> _materials;

        public MaterialIncomingWindow()
        {
            InitializeComponent();
            LoadMaterials();
            MaterialComboBox.SelectionChanged += MaterialComboBox_SelectionChanged;
        }

        private void LoadMaterials()
        {
            try
            {
                _materials = new List<Material>();

                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT material_id, material_name, material_stock FROM materials ORDER BY material_name";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _materials.Add(new Material
                            {
                                MaterialId = reader.GetInt32(0),
                                MaterialName = reader.GetString(1),
                                MaterialStock = reader.GetDecimal(2)
                            });
                        }
                    }
                }

                MaterialComboBox.ItemsSource = _materials;
                MaterialComboBox.DisplayMemberPath = "MaterialName";
                MaterialComboBox.SelectedValuePath = "MaterialId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки материалов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MaterialComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (MaterialComboBox.SelectedItem is Material selectedMaterial)
            {
                CurrentStockText.Text = $"{selectedMaterial.MaterialStock:F2} ед.";
            }
            else
            {
                CurrentStockText.Text = "-";
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка выбора материала
            if (MaterialComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите материал!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка количества
            if (!decimal.TryParse(QuantityTextBox.Text, out decimal quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество (больше 0)!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityTextBox.Focus();
                return;
            }

            int materialId = (int)MaterialComboBox.SelectedValue;
            string materialName = (MaterialComboBox.SelectedItem as Material)?.MaterialName ?? "";
            decimal oldStock = (MaterialComboBox.SelectedItem as Material)?.MaterialStock ?? 0;
            decimal newStock = oldStock + quantity;

            // Парсим цену (необязательно)
            decimal? price = null;
            if (!string.IsNullOrWhiteSpace(PriceTextBox.Text))
            {
                if (!decimal.TryParse(PriceTextBox.Text, out decimal p) || p <= 0)
                {
                    MessageBox.Show("Введите корректную цену (больше 0)!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    PriceTextBox.Focus();
                    return;
                }
                price = p;
            }

            string supplier = string.IsNullOrWhiteSpace(SupplierTextBox.Text) ? null : SupplierTextBox.Text.Trim();

            var result = MessageBox.Show($"Добавить {quantity} ед. материала \"{materialName}\" на склад?\n\n" +
                $"Текущий остаток: {oldStock:F2} ед.\n" +
                $"Новый остаток: {newStock:F2} ед.\n" +
                (price.HasValue ? $"Цена закупки: {price:F2} руб.\n" : "") +
                (supplier != null ? $"Поставщик: {supplier}\n" : ""),
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    // Обновляем остаток и информацию о последнем приходе
                    string updateSql = @"UPDATE materials SET 
                                        material_stock = material_stock + @qty,
                                        last_incoming_date = CURRENT_DATE,
                                        last_supplier = @supplier,
                                        last_incoming_quantity = @qty,
                                        last_incoming_price = @price
                                        WHERE material_id = @id";

                    using (var cmd = new NpgsqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@qty", quantity);
                        cmd.Parameters.AddWithValue("@id", materialId);
                        cmd.Parameters.AddWithValue("@supplier", supplier ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@price", price.HasValue ? price.Value : (object)DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"✅ Материал \"{materialName}\" пополнен на {quantity} ед.!\n" +
                    $"Новый остаток: {newStock:F2} ед.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при пополнении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}