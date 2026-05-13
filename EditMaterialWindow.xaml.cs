using System;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class EditMaterialWindow : Window
    {
        private Material _material;

        public EditMaterialWindow(Material material)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            _material = material;
            LoadMaterialData();
        }

        private void LoadMaterialData()
        {
            IdTextBox.Text = _material.MaterialId.ToString();
            NameTextBox.Text = _material.MaterialName;
            ManufacturerTextBox.Text = _material.MaterialManufacturer ?? "";
            PriceTextBox.Text = _material.MaterialPrice.ToString("F2");
            StockTextBox.Text = _material.MaterialStock.ToString("F2");
            ContraindicationsTextBox.Text = _material.MaterialContraindications ?? "";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите наименование материала!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("Введите корректную цену (неотрицательное число)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PriceTextBox.Focus();
                return;
            }

            if (!decimal.TryParse(StockTextBox.Text, out decimal stock) || stock < 0)
            {
                MessageBox.Show("Введите корректный остаток (неотрицательное число)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                StockTextBox.Focus();
                return;
            }

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    string sql = @"UPDATE materials SET 
                                  material_name = @name,
                                  material_manufacturer = @manufacturer,
                                  material_price = @price,
                                  material_contraindications = @contraindications,
                                  material_stock = @stock
                                  WHERE material_id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _material.MaterialId);
                        cmd.Parameters.AddWithValue("@name", NameTextBox.Text.Trim());

                        if (string.IsNullOrWhiteSpace(ManufacturerTextBox.Text))
                            cmd.Parameters.AddWithValue("@manufacturer", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@manufacturer", ManufacturerTextBox.Text.Trim());

                        cmd.Parameters.AddWithValue("@price", price);

                        if (string.IsNullOrWhiteSpace(ContraindicationsTextBox.Text))
                            cmd.Parameters.AddWithValue("@contraindications", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@contraindications", ContraindicationsTextBox.Text.Trim());

                        cmd.Parameters.AddWithValue("@stock", stock);

                        cmd.ExecuteNonQuery();
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении материала:\n{ex.Message}",
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