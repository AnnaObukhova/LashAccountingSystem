using System;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace LashAccountingSystem
{
    public class AppSettings
    {
        public bool AutoBackup { get; set; } = true;
        public bool ConfirmDelete { get; set; } = true;
        public bool LowStockWarning { get; set; } = true;
        public string BackupPath { get; set; } = "";
        public int LowStockThreshold { get; set; } = 5;
    }

    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;
        private string _configPath;

        public SettingsWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appDataPath, "LashAccountingSystem");

            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);

            _configPath = Path.Combine(appFolder, "config.txt");

            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string[] lines = File.ReadAllLines(_configPath);
                    _settings = new AppSettings();

                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string key = parts[0];
                            string value = parts[1];

                            switch (key)
                            {
                                case "AutoBackup": _settings.AutoBackup = bool.Parse(value); break;
                                case "ConfirmDelete": _settings.ConfirmDelete = bool.Parse(value); break;
                                case "LowStockWarning": _settings.LowStockWarning = bool.Parse(value); break;
                                case "BackupPath": _settings.BackupPath = value; break;
                                case "LowStockThreshold": _settings.LowStockThreshold = int.Parse(value); break;
                            }
                        }
                    }
                }
                else
                {
                    _settings = new AppSettings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки настроек: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                _settings = new AppSettings();
            }

            AutoBackupCheckBox.IsChecked = _settings.AutoBackup;
            ConfirmDeleteCheckBox.IsChecked = _settings.ConfirmDelete;
            LowStockWarningCheckBox.IsChecked = _settings.LowStockWarning;
            BackupPathTextBox.Text = _settings.BackupPath;
            LowStockThresholdTextBox.Text = _settings.LowStockThreshold.ToString();
        }

        private void SaveSettings()
        {
            try
            {
                _settings.AutoBackup = AutoBackupCheckBox.IsChecked ?? true;
                _settings.ConfirmDelete = ConfirmDeleteCheckBox.IsChecked ?? true;
                _settings.LowStockWarning = LowStockWarningCheckBox.IsChecked ?? true;
                _settings.BackupPath = BackupPathTextBox.Text;

                if (int.TryParse(LowStockThresholdTextBox.Text, out int threshold))
                    _settings.LowStockThreshold = threshold;

                var sb = new StringBuilder();
                sb.AppendLine($"AutoBackup={_settings.AutoBackup}");
                sb.AppendLine($"ConfirmDelete={_settings.ConfirmDelete}");
                sb.AppendLine($"LowStockWarning={_settings.LowStockWarning}");
                sb.AppendLine($"BackupPath={_settings.BackupPath}");
                sb.AppendLine($"LowStockThreshold={_settings.LowStockThreshold}");

                File.WriteAllText(_configPath, sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения настроек: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Сбросить все настройки на значения по умолчанию?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _settings = new AppSettings();
                if (File.Exists(_configPath))
                    File.Delete(_configPath);

                LoadSettings();
                MessageBox.Show("Настройки сброшены!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BrowseBackupButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            dialog.Title = "Выберите папку для резервных копий";

            if (dialog.ShowDialog() == true)
            {
                BackupPathTextBox.Text = dialog.FolderName;
            }
        }
    }
}