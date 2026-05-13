using System.Windows;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class ServiceDetailsWindow : Window
    {
        public ServiceDetailsWindow(Service service)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            ServiceNameText.Text = service.ServiceName;
            ServicePriceText.Text = $"{service.CurrentPrice:F2} руб.";
            ServiceDurationText.Text = $"{service.ServiceDuration} мин.";
            ServiceDescriptionText.Text = service.ServiceDescription ?? "Описание отсутствует";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}