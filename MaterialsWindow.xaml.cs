using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class MaterialsWindow : Window
    {
        private List<Material> _allMaterials;
        private List<Material> _filteredMaterials;

        public MaterialsWindow()
        {
            InitializeComponent();
            LoadMaterials();
        }

        private void LoadMaterials(string searchText = "")
        {
            try
            {
                _allMaterials = new List<Material>();

                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT material_id, material_name, material_manufacturer, " +
                                 "material_price, material_contraindications, material_stock " +
                                 "FROM materials ORDER BY material_name;";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _allMaterials.Add(new Material
                            {
                                MaterialId = reader.GetInt32(0),
                                MaterialName = reader.GetString(1),
                                MaterialManufacturer = reader.IsDBNull(2) ? null : reader.GetString(2),
                                MaterialPrice = reader.GetDecimal(3),
                                MaterialContraindications = reader.IsDBNull(4) ? null : reader.GetString(4),
                                MaterialStock = reader.GetDecimal(5)
                            });
                        }
                    }
                }

                ApplyFilter(searchText);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки материалов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredMaterials = _allMaterials;
            }
            else
            {
                string lowerSearch = searchText.ToLower();
                _filteredMaterials = _allMaterials.Where(m =>
                    (m.MaterialName?.ToLower().Contains(lowerSearch) ?? false) ||
                    (m.MaterialManufacturer?.ToLower().Contains(lowerSearch) ?? false)
                ).ToList();
            }

            MaterialsDataGrid.ItemsSource = _filteredMaterials;

            if (SearchResultCount != null)
            {
                SearchResultCount.Text = $"Найдено: {_filteredMaterials.Count} из {_allMaterials.Count}";
            }
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
            var addWindow = new AddMaterialWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadMaterials(SearchTextBox?.Text ?? "");
                MessageBox.Show("Материал успешно добавлен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (MaterialsDataGrid.SelectedItem is Material selectedMaterial)
            {
                var editWindow = new EditMaterialWindow(selectedMaterial);
                if (editWindow.ShowDialog() == true)
                {
                    LoadMaterials(SearchTextBox?.Text ?? "");
                    MessageBox.Show("Материал успешно обновлён!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Выберите материал для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (MaterialsDataGrid.SelectedItem is Material selectedMaterial)
            {
                var result = MessageBox.Show($"Удалить материал \"{selectedMaterial.MaterialName}\"?\n\n" +
                    "Внимание! Будут удалены также связанные записи о расходе.",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var conn = DbConnection.GetConnection())
                        {
                            conn.Open();
                            string sql = "DELETE FROM materials WHERE material_id = @id";
                            using (var cmd = new NpgsqlCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", selectedMaterial.MaterialId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        LoadMaterials(SearchTextBox?.Text ?? "");
                        MessageBox.Show("Материал удалён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Выберите материал для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadMaterials(SearchTextBox?.Text ?? "");
            MessageBox.Show("Список материалов обновлён!", "Обновление",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}