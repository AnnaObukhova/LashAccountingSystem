using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;

namespace LashAccountingSystem
{
    public partial class MaterialReportWindow : Window
    {
        public MaterialReportWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            StartDatePicker.SelectedDate = firstDayOfMonth;
            EndDatePicker.SelectedDate = today;

            LoadReports();
        }

        private void LoadReports()
        {
            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите период!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime startDate = StartDatePicker.SelectedDate.Value;
            DateTime endDate = EndDatePicker.SelectedDate.Value.AddDays(1);

            LoadIncomingReport(startDate, endDate);
            LoadOutgoingReport(startDate, endDate);
            LoadSummaryReport(startDate, endDate);
        }

        /// <summary>
        /// Приход материалов (агрегированный)
        /// </summary>
        private void LoadIncomingReport(DateTime startDate, DateTime endDate)
        {
            var incomingData = new List<IncomingItem>();

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT 
                            m.material_name,
                            COALESCE(m.material_manufacturer, '-') as manufacturer,
                            COALESCE(m.last_incoming_price, m.material_price, 0) as price,
                            COALESCE(m.last_incoming_quantity, 0) as quantity
                        FROM materials m
                        WHERE m.last_incoming_date BETWEEN @startDate AND @endDate
                           OR (m.last_incoming_quantity > 0 AND m.last_incoming_date IS NOT NULL)
                        ORDER BY m.material_name";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@startDate", startDate);
                        cmd.Parameters.AddWithValue("@endDate", endDate);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                decimal quantity = reader.GetDecimal(3);
                                decimal price = reader.GetDecimal(2);
                                incomingData.Add(new IncomingItem
                                {
                                    MaterialName = reader.GetString(0),
                                    Manufacturer = reader.GetString(1),
                                    Price = price,
                                    Quantity = quantity,
                                    Total = quantity * price
                                });
                            }
                        }
                    }
                }
                IncomingDataGrid.ItemsSource = incomingData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки прихода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Расход материалов (агрегированный по материалу)
        /// </summary>
        private void LoadOutgoingReport(DateTime startDate, DateTime endDate)
        {
            var outgoingDict = new Dictionary<string, OutgoingItem>();

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
                            m.material_price as price,
                            SUM(mc.material_consumption_amount) as total_quantity
                        FROM materials_consumption mc
                        JOIN materials m ON mc.material_id = m.material_id
                        JOIN appointments a ON mc.appointment_id = a.appointment_id
                        WHERE a.appointment_date BETWEEN @startDate AND @endDate
                        GROUP BY m.material_id, m.material_name, m.material_manufacturer, m.material_price
                        ORDER BY m.material_name";

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
                                string materialName = reader.GetString(1);
                                string manufacturer = reader.GetString(2);

                                outgoingDict[materialName] = new OutgoingItem
                                {
                                    MaterialName = materialName,
                                    Manufacturer = manufacturer,
                                    Price = price,
                                    Quantity = quantity,
                                    Total = quantity * price
                                };
                            }
                        }
                    }
                }

                var outgoingList = new List<OutgoingItem>(outgoingDict.Values);
                OutgoingDataGrid.ItemsSource = outgoingList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки расхода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Сводная ведомость
        /// </summary>
        private void LoadSummaryReport(DateTime startDate, DateTime endDate)
        {
            var summaryData = new List<SummaryItem>();

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    // Приход за период
                    var incomingMap = new Dictionary<string, decimal>();
                    string incomingSql = @"
                        SELECT 
                            m.material_name,
                            COALESCE(m.last_incoming_quantity, 0) as quantity
                        FROM materials m
                        WHERE m.last_incoming_date BETWEEN @startDate AND @endDate";

                    using (var cmd = new NpgsqlCommand(incomingSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@startDate", startDate);
                        cmd.Parameters.AddWithValue("@endDate", endDate);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                incomingMap[reader.GetString(0)] = reader.GetDecimal(1);
                            }
                        }
                    }

                    // Расход за период
                    var outgoingMap = new Dictionary<string, decimal>();
                    string outgoingSql = @"
                        SELECT 
                            m.material_name,
                            SUM(mc.material_consumption_amount) as total_quantity
                        FROM materials_consumption mc
                        JOIN materials m ON mc.material_id = m.material_id
                        JOIN appointments a ON mc.appointment_id = a.appointment_id
                        WHERE a.appointment_date BETWEEN @startDate AND @endDate
                        GROUP BY m.material_name";

                    using (var cmd = new NpgsqlCommand(outgoingSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@startDate", startDate);
                        cmd.Parameters.AddWithValue("@endDate", endDate);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                outgoingMap[reader.GetString(0)] = reader.GetDecimal(1);
                            }
                        }
                    }

                    // Все материалы
                    string materialsSql = @"
                        SELECT 
                            m.material_name,
                            COALESCE(m.material_manufacturer, '-') as manufacturer,
                            m.material_stock as stock,
                            COALESCE(m.last_incoming_price, m.material_price, 0) as avg_price
                        FROM materials m
                        ORDER BY m.material_name";

                    using (var cmd = new NpgsqlCommand(materialsSql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = reader.GetString(0);
                            decimal incoming = incomingMap.ContainsKey(name) ? incomingMap[name] : 0;
                            decimal outgoing = outgoingMap.ContainsKey(name) ? outgoingMap[name] : 0;
                            decimal stock = reader.GetDecimal(2);
                            decimal avgPrice = reader.GetDecimal(3);

                            summaryData.Add(new SummaryItem
                            {
                                MaterialName = name,
                                Manufacturer = reader.GetString(1),
                                Incoming = incoming,
                                Outgoing = outgoing,
                                Stock = stock,
                                AvgPrice = avgPrice,
                                StockValue = stock * avgPrice
                            });
                        }
                    }
                }
                SummaryDataGrid.ItemsSource = summaryData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сводки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите начальную и конечную даты!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (StartDatePicker.SelectedDate > EndDatePicker.SelectedDate)
            {
                MessageBox.Show("Начальная дата не может быть позже конечной!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadReports();
        }

        private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var summaryData = SummaryDataGrid.ItemsSource as List<SummaryItem>;
                if (summaryData == null || summaryData.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта!", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string fileName = $"Отчёт_по_материалам_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string filePath = Path.Combine(documentsPath, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("Материал;Производитель;Приход;Расход;Остаток;Ср.цена закупки;Стоимость остатка");

                foreach (var item in summaryData)
                {
                    sb.AppendLine($"{item.MaterialName};{item.Manufacturer};{item.Incoming:F2};{item.Outgoing:F2};{item.Stock:F2};{item.AvgPrice:F2};{item.StockValue:F2}");
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

                MessageBox.Show($"✅ Отчёт экспортирован в CSV!\n\n📁 {filePath}", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var summaryData = SummaryDataGrid.ItemsSource as List<SummaryItem>;
                if (summaryData == null || summaryData.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта!", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var htmlBuilder = new StringBuilder();

                htmlBuilder.AppendLine("<!DOCTYPE html>");
                htmlBuilder.AppendLine("<html>");
                htmlBuilder.AppendLine("<head>");
                htmlBuilder.AppendLine("<meta charset='UTF-8'>");
                htmlBuilder.AppendLine("<style>");
                htmlBuilder.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; margin: 20px; }");
                htmlBuilder.AppendLine("h1 { color: #3D5736; text-align: center; }");
                htmlBuilder.AppendLine("h3 { text-align: center; color: #666; }");
                htmlBuilder.AppendLine("table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
                htmlBuilder.AppendLine("th { background-color: #FDF0F4; border: 1px solid #D2D5AB; padding: 8px; text-align: center; }");
                htmlBuilder.AppendLine("td { border: 1px solid #D2D5AB; padding: 6px; }");
                htmlBuilder.AppendLine(".right { text-align: right; }");
                htmlBuilder.AppendLine(".footer { font-size: 10px; text-align: center; margin-top: 30px; color: #999; }");
                htmlBuilder.AppendLine("</style>");
                htmlBuilder.AppendLine("</head>");
                htmlBuilder.AppendLine("<body>");

                htmlBuilder.AppendLine($"<h1>📊 Отчёт по движению материалов</h1>");
                htmlBuilder.AppendLine($"<h3>Период: {StartDatePicker.SelectedDate:dd.MM.yyyy} - {EndDatePicker.SelectedDate:dd.MM.yyyy}</h3>");

                htmlBuilder.AppendLine("<table>");
                htmlBuilder.AppendLine("<tr><th>Материал</th><th>Производитель</th><th>Приход</th><th>Расход</th><th>Остаток</th><th>Ср.цена закупки</th><th>Стоимость остатка</th></tr>");

                foreach (var item in summaryData)
                {
                    htmlBuilder.AppendLine($"<tr>");
                    htmlBuilder.AppendLine($"<td>{System.Security.SecurityElement.Escape(item.MaterialName)}</td>");
                    htmlBuilder.AppendLine($"<td>{System.Security.SecurityElement.Escape(item.Manufacturer)}</td>");
                    htmlBuilder.AppendLine($"<td class='right'>{item.Incoming:F2}</td>");
                    htmlBuilder.AppendLine($"<td class='right'>{item.Outgoing:F2}</td>");
                    htmlBuilder.AppendLine($"<td class='right'>{item.Stock:F2}</td>");
                    htmlBuilder.AppendLine($"<td class='right'>{item.AvgPrice:F2}</td>");
                    htmlBuilder.AppendLine($"<td class='right'>{item.StockValue:F2}</td>");
                    htmlBuilder.AppendLine($"</tr>");
                }

                htmlBuilder.AppendLine("</table>");
                htmlBuilder.AppendLine($"<p class='footer'>Дата формирования отчёта: {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>");
                htmlBuilder.AppendLine("</body>");
                htmlBuilder.AppendLine("</html>");

                string tempHtml = Path.GetTempFileName() + ".html";
                File.WriteAllText(tempHtml, htmlBuilder.ToString(), Encoding.UTF8);

                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = tempHtml;
                    process.StartInfo.UseShellExecute = true;
                    process.Start();
                }

                MessageBox.Show("✅ Отчёт сформирован и открыт в браузере.\n\n" +
                    "Для сохранения в PDF нажмите Ctrl+P и выберите 'Microsoft Print to PDF'.",
                    "Экспорт в PDF", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта в PDF: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // ============================================
    // МОДЕЛИ ДАННЫХ
    // ============================================

    public class IncomingItem
    {
        public string MaterialName { get; set; }
        public string Manufacturer { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal Total { get; set; }
    }

    public class OutgoingItem
    {
        public string MaterialName { get; set; }
        public string Manufacturer { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal Total { get; set; }
    }

    public class SummaryItem
    {
        public string MaterialName { get; set; }
        public string Manufacturer { get; set; }
        public decimal Incoming { get; set; }
        public decimal Outgoing { get; set; }
        public decimal Stock { get; set; }
        public decimal AvgPrice { get; set; }
        public decimal StockValue { get; set; }
    }
}