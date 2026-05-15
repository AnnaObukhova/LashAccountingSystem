using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class MaterialReportWindow : Window
    {
        private List<MaterialReportItem> _reportData;

        public MaterialReportWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            // Устанавливаем даты по умолчанию (начало и конец текущего месяца)
            var today = DateTime.Today;
            StartDatePicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
            EndDatePicker.SelectedDate = today;

            LoadReport();
        }

        private void LoadReport()
        {
            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите период!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime startDate = StartDatePicker.SelectedDate.Value;
            DateTime endDate = EndDatePicker.SelectedDate.Value.AddDays(1);

            _reportData = new List<MaterialReportItem>();

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    string sql = @"
                SELECT 
                    m.material_id,
                    m.material_name,
                    COALESCE(m.material_manufacturer, '-') as manufacturer,
                    COALESCE(m.material_price, 0) as price,
                    SUM(mc.material_consumption_amount) as total_quantity,
                    COUNT(DISTINCT mc.appointment_id) as appointment_count,
                    m.last_incoming_date,
                    m.last_supplier,
                    m.last_incoming_price,
                    m.material_stock as current_stock
                FROM materials_consumption mc
                JOIN materials m ON mc.material_id = m.material_id
                JOIN appointments a ON mc.appointment_id = a.appointment_id
                WHERE a.appointment_date >= @startDate 
                  AND a.appointment_date < @endDate
                GROUP BY m.material_id, m.material_name, m.material_manufacturer, 
                         m.material_price, m.last_incoming_date, m.last_supplier, 
                         m.last_incoming_price, m.material_stock
                ORDER BY total_quantity DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@startDate", startDate);
                        cmd.Parameters.AddWithValue("@endDate", endDate);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                decimal quantity = reader.GetDecimal(4);
                                decimal price = reader.GetDecimal(3);

                                _reportData.Add(new MaterialReportItem
                                {
                                    MaterialId = reader.GetInt32(0),
                                    MaterialName = reader.GetString(1),
                                    Manufacturer = reader.GetString(2),
                                    Price = price,
                                    TotalQuantity = quantity,
                                    TotalCost = quantity * price,
                                    AppointmentCount = reader.GetInt32(5),
                                    LastIncomingDate = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                                    LastSupplier = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    LastIncomingPrice = reader.IsDBNull(8) ? null : reader.GetDecimal(8),
                                    CurrentStock = reader.GetDecimal(9)
                                });
                            }
                        }
                    }
                }

                UpdateUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчёта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI()
        {
            ReportDataGrid.ItemsSource = _reportData;

            // Подсчёт итогов
            int totalMaterials = _reportData.Count;
            decimal totalQuantity = 0;
            decimal totalCost = 0;

            foreach (var item in _reportData)
            {
                totalQuantity += item.TotalQuantity;
                totalCost += item.TotalCost;
            }

            TotalMaterialsCount.Text = $"📦 Всего материалов: {totalMaterials}";
            TotalQuantity.Text = $"🔢 Общий расход: {totalQuantity:F2} ед.";
            TotalCost.Text = $"💰 Общая стоимость: {totalCost:F2} руб.";
        }

        private void ShowReportButton_Click(object sender, RoutedEventArgs e)
        {
            LoadReport();
        }

        private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            if (_reportData == null || _reportData.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string fileName = $"Отчет_по_материалам_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string filePath = Path.Combine(documentsPath, fileName);

                var sb = new StringBuilder();

                // Заголовки (расширенные)
                sb.AppendLine("Материал;Производитель;Расход (ед.);Цена продажи (руб);Последний приход;Поставщик;Цена закупки (руб);Остаток на складе;Общая стоимость (руб);Кол-во записей");

                // Данные
                foreach (var item in _reportData)
                {
                    sb.AppendLine($"{item.MaterialName};{item.Manufacturer};{item.TotalQuantity:F2};{item.Price:F2};" +
                        $"{(item.LastIncomingDate?.ToString("dd.MM.yyyy") ?? "-")};{item.LastSupplier ?? "-"};" +
                        $"{(item.LastIncomingPrice?.ToString("F2") ?? "-")};{item.CurrentStock:F2};{item.TotalCost:F2};{item.AppointmentCount}");
                }

                // Итоги
                sb.AppendLine();
                sb.AppendLine($"Всего материалов;{_reportData.Count};;;;;;;;");
                sb.AppendLine($"Общий расход;;{TotalQuantity.Text.Split(':')[1].Trim()};;;;;;;");
                sb.AppendLine($"Общая стоимость;;;;;;;{TotalCost.Text.Split(':')[1].Trim()};;");

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

                MessageBox.Show($"✅ Отчёт экспортирован!\n\n📁 {filePath}", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}