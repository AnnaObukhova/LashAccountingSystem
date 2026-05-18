using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;

namespace LashAccountingSystem
{
    public partial class BackupWindow : Window
    {
        private string BackupFolderPath;

        public BackupWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            BackupFolderPath = Path.Combine(documentsPath, "LashAccountingSystem", "Backups");

            if (!Directory.Exists(BackupFolderPath))
            {
                Directory.CreateDirectory(BackupFolderPath);
            }

            LoadBackupsList();
        }

        private void LoadBackupsList()
        {
            try
            {
                var backupFiles = Directory.GetFiles(BackupFolderPath, "*.sql");
                Array.Sort(backupFiles);
                Array.Reverse(backupFiles);

                BackupsListBox.Items.Clear();
                foreach (var file in backupFiles)
                {
                    FileInfo info = new FileInfo(file);
                    string fileName = Path.GetFileName(file);
                    string size = GetFileSize(info.Length);
                    string date = info.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss");
                    BackupsListBox.Items.Add($"[{date}] {fileName} ({size})");
                }

                if (backupFiles.Length == 0)
                {
                    BackupsListBox.Items.Add("Нет сохранённых копий");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка: {ex.Message}");
            }
        }

        private string GetFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        // Создание резервной копии с показом окна
        private void CreateBackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string fileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
                string filePath = Path.Combine(BackupFolderPath, fileName);
                CreateBackupInternal(filePath);

                MessageBox.Show($"✅ Резервная копия создана!\n\n📁 {filePath}\n📊 Размер: {GetFileSize(new FileInfo(filePath).Length)}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadBackupsList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания бэкапа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Создание резервной копии без показа окна (для авто-бэкапа)
        public void CreateBackupSilent(string filePath)
        {
            try
            {
                CreateBackupInternal(filePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка авто-бэкапа: {ex.Message}");
            }
        }

        // Внутренний метод создания резервной копии
        private void CreateBackupInternal(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-- Резервная копия LashMasterDatabase");
            sb.AppendLine($"-- Дата: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine();

            using (var conn = DbConnection.GetConnection())
            {
                conn.Open();

                // Экспортируем все таблицы
                ExportTable(conn, "clients", sb);
                ExportTable(conn, "masters", sb);
                ExportTable(conn, "services", sb);
                ExportTable(conn, "materials", sb);
                ExportTable(conn, "service_materials", sb);
                ExportTable(conn, "price_history", sb);
                ExportTable(conn, "appointments", sb);
                ExportTable(conn, "materials_consumption", sb);
                ExportTable(conn, "users", sb);
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        // Экспорт одной таблицы в SQL
        private void ExportTable(NpgsqlConnection conn, string tableName, StringBuilder sb)
        {
            try
            {
                sb.AppendLine($"-- Таблица: {tableName}");
                string sql = $"SELECT * FROM {tableName}";
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    var schema = reader.GetColumnSchema();
                    var columnNames = new List<string>();
                    foreach (var col in schema)
                    {
                        columnNames.Add(col.ColumnName);
                    }
                    string columns = string.Join(", ", columnNames);

                    while (reader.Read())
                    {
                        var values = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            object value = reader.GetValue(i);
                            values.Add(FormatSqlValue(value));
                        }
                        string valuesStr = string.Join(", ", values);
                        sb.AppendLine($"INSERT INTO {tableName} ({columns}) VALUES ({valuesStr});");
                    }
                }
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                sb.AppendLine($"-- Ошибка экспорта {tableName}: {ex.Message}");
                sb.AppendLine();
            }
        }

        private string FormatSqlValue(object value)
        {
            if (value == null || value == DBNull.Value)
                return "NULL";
            if (value is string s)
                return $"'{s.Replace("'", "''")}'";
            if (value is DateTime dt)
                return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
            if (value is bool b)
                return b ? "TRUE" : "FALSE";
            if (value is int || value is long || value is decimal || value is float || value is double)
                return value.ToString().Replace(',', '.');
            return $"'{value.ToString().Replace("'", "''")}'";
        }

        // Восстановление базы данных из SQL-дампа
        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка: выбран ли файл для восстановления
            if (BackupsListBox.SelectedIndex == -1 || BackupsListBox.SelectedItem.ToString() == "Нет сохранённых копий")
            {
                MessageBox.Show("Выберите резервную копию!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Получаем путь к выбранному файлу
            string selected = BackupsListBox.SelectedItem.ToString();
            string fileName = ExtractFileNameFromDisplay(selected);
            string filePath = Path.Combine(BackupFolderPath, fileName);

            // Запрашиваем подтверждение у пользователя
            var result = MessageBox.Show($"⚠️ Восстановление УДАЛИТ все текущие данные!\n\nПродолжить?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                string sqlContent = File.ReadAllText(filePath, Encoding.UTF8);   // Читаем SQL-дамп

                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Отключаем проверку внешних ключей для безопасного удаления
                            using (var cmd = new NpgsqlCommand("SET session_replication_role = 'replica';", conn, transaction))
                            {
                                cmd.ExecuteNonQuery();
                            }

                            // Очищаем таблицы в правильном порядке (сначала зависимые, потом главные)
                            string[] tables = { "materials_consumption", "appointments", "service_materials",
                                       "price_history", "users", "materials", "services", "masters", "clients" };

                            foreach (var table in tables)
                            {
                                using (var cmd = new NpgsqlCommand($"TRUNCATE TABLE {table} RESTART IDENTITY CASCADE;", conn, transaction))
                                {
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            // Выполняем INSERT-запросы из дампа
                            string[] lines = sqlContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string line in lines)
                            {
                                string trimmed = line.Trim();
                                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                                if (trimmed.StartsWith("--")) continue;           // Пропускаем комментарии

                                if (trimmed.StartsWith("INSERT INTO"))           // Только INSERT-запросы
                                {
                                    using (var cmd = new NpgsqlCommand(trimmed, conn, transaction))
                                    {
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }

                            // Включаем обратно проверку внешних ключей
                            using (var cmd = new NpgsqlCommand("SET session_replication_role = 'origin';", conn, transaction))
                            {
                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();                                 // Фиксируем изменения
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();                               // Откат при ошибке
                            throw;
                        }
                    }
                }

                MessageBox.Show("✅ База данных восстановлена!\n\nПриложение будет перезапущено.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Перезапуск приложения
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка восстановления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ExtractFileNameFromDisplay(string displayText)
        {
            int startIndex = displayText.IndexOf(']') + 2;
            int endIndex = displayText.LastIndexOf('(') - 1;
            if (endIndex > startIndex)
            {
                return displayText.Substring(startIndex, endIndex - startIndex).Trim();
            }
            return "";
        }

        private void BackupsListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (BackupsListBox.SelectedIndex != -1 &&
                BackupsListBox.SelectedItem.ToString() != "Нет сохранённых копий")
            {
                RestoreButton_Click(sender, e);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}