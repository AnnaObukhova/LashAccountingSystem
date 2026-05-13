using System;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;

namespace LashAccountingSystem
{
    public partial class AddMaterialWindow : Window
    {
        public AddMaterialWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
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

            decimal stock = 0;
            if (!string.IsNullOrWhiteSpace(StockTextBox.Text))
            {
                if (!decimal.TryParse(StockTextBox.Text, out stock) || stock < 0)
                {
                    MessageBox.Show("Введите корректный остаток (неотрицательное число)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StockTextBox.Focus();
                    return;
                }
            }

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    string sql = @"INSERT INTO materials 
                                  (material_name, material_manufacturer, material_price, 
                                   material_contraindications, material_stock) 
                                  VALUES (@name, @manufacturer, @price, @contraindications, @stock)";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
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