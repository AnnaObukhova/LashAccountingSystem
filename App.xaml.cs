using System;
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
            base.OnStartup(e);

            try
            {
                bool hasUsers = HasUsers();

                if (!hasUsers)
                {
                    // Первый запуск — регистрация
                    var registerWindow = new RegisterWindow();
                    registerWindow.ShowDialog();

                    if (!registerWindow.IsRegistrationComplete)
                    {
                        Shutdown();
                        return;
                    }
                }

                // Вход в систему
                var loginWindow = new LoginWindow();
                bool? result = loginWindow.ShowDialog();

                if (result == true && loginWindow.IsLoggedIn)
                {
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
                MessageBox.Show($"Ошибка: {ex.Message}");
                Shutdown();
            }
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