using System;
using System.Collections.Generic;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;

namespace LashAccountingSystem
{
    public partial class PriceHistoryWindow : Window
    {
        public PriceHistoryWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            LoadPriceHistory();
        }

        private void LoadPriceHistory()
        {
            var priceHistory = new List<dynamic>();

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT 
                            ph.price_history_id AS ID,
                            s.service_name AS Услуга,
                            ph.price_history_price AS Цена,
                            ph.price_history_application_date AS ""Дата применения""
                        FROM price_history ph
                        JOIN services s ON ph.service_id = s.service_id
                        ORDER BY ph.price_history_application_date DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            priceHistory.Add(new
                            {
                                ID = reader.GetInt32(0),
                                Услуга = reader.GetString(1),
                                Цена = reader.GetDecimal(2),
                                ДатаПрименения = reader.GetDateTime(3).ToString("dd.MM.yyyy")
                            });
                        }
                    }
                }

                PriceHistoryDataGrid.ItemsSource = priceHistory;

                if (priceHistory.Count == 0)
                {
                    MessageBox.Show("История цен пуста. Добавьте цены через окно управления услугами.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории цен: {ex.Message}", "Ошибка");
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadPriceHistory();
        }
    }
}