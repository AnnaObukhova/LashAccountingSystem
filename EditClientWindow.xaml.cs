using System;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class EditClientWindow : Window
    {
        private Client _client;

        public EditClientWindow(Client client)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            _client = client;
            LoadClientData();
        }

        private void LoadClientData()
        {
            IdTextBox.Text = _client.ClientId.ToString();
            SurnameTextBox.Text = _client.ClientSurname;
            NameTextBox.Text = _client.ClientName;
            PatronymicTextBox.Text = _client.ClientPatronymic ?? "";
            PhoneTextBox.Text = _client.ClientPhoneNumber;
            EmailTextBox.Text = _client.ClientEmail ?? "";
            NotesTextBox.Text = _client.ClientHealthFeatures ?? "";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(SurnameTextBox.Text))
            {
                MessageBox.Show("Введите фамилию клиента!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                SurnameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите имя клиента!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                MessageBox.Show("Введите номер телефона клиента!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PhoneTextBox.Focus();
                return;
            }

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    string sql = @"UPDATE clients SET 
                                  client_surname = @surname,
                                  client_name = @name,
                                  client_patronymic = @patronymic,
                                  client_phone_number = @phone,
                                  client_email = @email,
                                  client_health_features = @health
                                  WHERE client_id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _client.ClientId);
                        cmd.Parameters.AddWithValue("@surname", SurnameTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@name", NameTextBox.Text.Trim());

                        if (string.IsNullOrWhiteSpace(PatronymicTextBox.Text))
                            cmd.Parameters.AddWithValue("@patronymic", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@patronymic", PatronymicTextBox.Text.Trim());

                        cmd.Parameters.AddWithValue("@phone", PhoneTextBox.Text.Trim());

                        if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                            cmd.Parameters.AddWithValue("@email", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@email", EmailTextBox.Text.Trim());

                        if (string.IsNullOrWhiteSpace(NotesTextBox.Text))
                            cmd.Parameters.AddWithValue("@health", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@health", NotesTextBox.Text.Trim());

                        cmd.ExecuteNonQuery();
                    }
                }

                // Обновляем данные в переданном объекте
                _client.ClientSurname = SurnameTextBox.Text.Trim();
                _client.ClientName = NameTextBox.Text.Trim();
                _client.ClientPatronymic = string.IsNullOrWhiteSpace(PatronymicTextBox.Text) ? null : PatronymicTextBox.Text.Trim();
                _client.ClientPhoneNumber = PhoneTextBox.Text.Trim();
                _client.ClientEmail = string.IsNullOrWhiteSpace(EmailTextBox.Text) ? null : EmailTextBox.Text.Trim();
                _client.ClientHealthFeatures = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении клиента:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}