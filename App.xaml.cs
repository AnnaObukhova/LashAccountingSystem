using System;
using System.IO;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class App : Application
    {
        public static User CurrentUser { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Перехват всех необработанных исключений
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            base.OnStartup(e);

            try
            {
                bool hasUsers = HasUsers();

                if (!hasUsers)
                {
                    var registerWindow = new RegisterWindow();
                    registerWindow.ShowDialog();

                    if (!registerWindow.IsRegistrationComplete)
                    {
                        Shutdown();
                        return;
                    }
                }

                var loginWindow = new LoginWindow();
                bool? result = loginWindow.ShowDialog();

                if (result == true && loginWindow.IsLoggedIn)
                {
                    // ============================================
                    // ПРОВЕРКА ЦЕЛОСТНОСТИ БАЗЫ ДАННЫХ
                    // ============================================
                    if (!CheckDatabaseIntegrity())
                    {
                        var restoreResult = MessageBox.Show(
                            "Обнаружены проблемы с базой данных!\n\n" +
                            "Восстановить данные из последней резервной копии?",
                            "Ошибка целостности БД",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Error);

                        if (restoreResult == MessageBoxResult.Yes)
                        {
                            // Открываем окно восстановления
                            var backupWindow = new BackupWindow();
                            backupWindow.ShowDialog();
                        }
                        else
                        {
                            MessageBox.Show("Продолжение работы может привести к ошибкам!",
                                "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }

                    // ============================================
                    // АВТОМАТИЧЕСКОЕ СОЗДАНИЕ РЕЗЕРВНОЙ КОПИИ (РАЗ В СУТКИ)
                    // ============================================
                    CreateAutoBackupIfNeeded();

                    var mainWindow = new ScheduleWindow();
                    MainWindow = mainWindow;
                    mainWindow.Show();
                }
                else
                {
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске: {ex.Message}\n{ex.StackTrace}",
                    "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        /// <summary>
        /// Проверка целостности базы данных
        /// </summary>
        private bool CheckDatabaseIntegrity()
        {
            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    // Простая проверка: выполнить запрос
                    string sql = "SELECT 1";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.ExecuteScalar();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к БД: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Автоматическое создание резервной копии (раз в сутки)
        /// </summary>
        private void CreateAutoBackupIfNeeded()
        {
            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string backupFolder = Path.Combine(documentsPath, "LashAccountingSystem", "Backups");

                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                // Ищем сегодняшний бэкап
                string today = DateTime.Now.ToString("yyyyMMdd");
                var todayBackup = Directory.GetFiles(backupFolder, $"backup_{today}*.sql");

                if (todayBackup.Length == 0)
                {
                    // Создаём автоматический бэкап
                    string fileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}_auto.sql";
                    string filePath = Path.Combine(backupFolder, fileName);

                    // Используем существующий метод создания бэкапа
                    var backupWindow = new BackupWindow();
                    backupWindow.CreateBackupSilent(filePath);

                    // Можно добавить лог
                    System.Diagnostics.Debug.WriteLine($"Автоматический бэкап создан: {filePath}");
                }
            }
            catch (Exception ex)
            {
                // Не показываем ошибку пользователю, только в лог
                System.Diagnostics.Debug.WriteLine($"Ошибка авто-бэкапа: {ex.Message}");
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show($"Необработанная ошибка: {ex?.Message}\n{ex?.StackTrace}",
                "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Ошибка: {e.Exception.Message}\n{e.Exception.StackTrace}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Чтобы приложение не закрывалось
        }

        private bool HasUsers()
        {
            using (var conn = DbConnection.GetConnection())
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM users";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    long count = (long)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }
    }
}