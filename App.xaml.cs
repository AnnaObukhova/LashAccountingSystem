using LashAccountingSystem.Models;
using System.Windows;

namespace LashAccountingSystem
{
    public partial class App : Application
    {
        public static User CurrentUser { get; set; }
        private ScheduleWindow _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Создаём главное окно (но не показываем)
            _mainWindow = new ScheduleWindow();

            // Создаём окно входа
            var loginWindow = new LoginWindow();

            // Показываем окно входа
            if (loginWindow.ShowDialog() == true && loginWindow.IsLoggedIn)
            {
                // Вход успешен — показываем главное окно
                _mainWindow.Show();
            }
            else
            {
                // Вход не выполнен — закрываем приложение
                _mainWindow.Close();
                Shutdown();
            }
        }
    }
}