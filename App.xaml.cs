using LashAccountingSystem.Models;
using System.Windows;

namespace LashAccountingSystem
{
    public partial class App : Application
    {
        public static User CurrentUser { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var loginWindow = new LoginWindow();

            if (loginWindow.ShowDialog() == true && loginWindow.IsLoggedIn)
            {
                var mainWindow = new ScheduleWindow();
                MainWindow = mainWindow;
                mainWindow.Show();

                // Отладочное сообщение
                MessageBox.Show("Главное окно должно быть открыто");
            }
            else
            {
                Shutdown();
            }
        }
    }
}