using System;
using System.Windows;
using LashAccountingSystem.Models;
using Npgsql;

namespace LashAccountingSystem
{
    public partial class App : Application
    {
        public static User CurrentUser { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Глобальная обработка необработанных исключений
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            var mainWindow = new ScheduleWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Критическая ошибка: {e.ExceptionObject}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Ошибка: {e.Exception.Message}\n{e.Exception.StackTrace}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}