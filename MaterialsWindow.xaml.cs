using System;
using System.Collections.Generic;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class MaterialsWindow : Window
    {
        public MaterialsWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            LoadMaterials();
        }

        private void LoadMaterials()
        {
            List<Material> materials = new List<Material>();

            try
            {
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
                            materials.Add(new Material
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

                MaterialsDataGrid.ItemsSource = materials;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки материалов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddMaterialWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadMaterials();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (MaterialsDataGrid.SelectedItem is Material selectedMaterial)
            {
                var editWindow = new EditMaterialWindow(selectedMaterial);
                if (editWindow.ShowDialog() == true)
                {
                    LoadMaterials();
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
                        LoadMaterials();
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
            LoadMaterials();
            MessageBox.Show("Список материалов обновлён!", "Обновление",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}