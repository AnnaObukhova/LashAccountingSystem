using System;
using System.Windows;
using Npgsql;
using LashAccountingSystem.Database;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class AddEditMasterWindow : Window
    {
        private Master _editingMaster;
        private bool _isEditMode = false;

        public AddEditMasterWindow(Master master = null)
        {
            InitializeComponent();

            if (master != null)
            {
                _isEditMode = true;
                _editingMaster = master;
                TitleText.Text = "✏️ Редактирование мастера";
                LoadMasterData();
            }
        }

        private void LoadMasterData()
        {
            SurnameTextBox.Text = _editingMaster.MasterSurname;
            NameTextBox.Text = _editingMaster.MasterName;
            PatronymicTextBox.Text = _editingMaster.MasterPatronymic ?? "";
            PhoneTextBox.Text = _editingMaster.MasterPhoneNumber;
            ContractNumberTextBox.Text = _editingMaster.MasterContractNumber;
            SpecializationTextBox.Text = _editingMaster.MasterSpecialization ?? "";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(SurnameTextBox.Text))
            {
                MessageBox.Show("Введите фамилию мастера!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SurnameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите имя мастера!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                MessageBox.Show("Введите телефон мастера!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PhoneTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(ContractNumberTextBox.Text))
            {
                MessageBox.Show("Введите номер договора мастера!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ContractNumberTextBox.Focus();
                return;
            }

            try
            {
                using (var conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    if (_isEditMode)
                    {
                        // Обновление
                        string sql = @"UPDATE masters SET 
                                        master_surname = @surname,
                                        master_name = @name,
                                        master_patronymic = @patronymic,
                                        master_phone_number = @phone,
                                        master_contract_number = @contract,
                                        master_specialization = @specialization
                                      WHERE master_id = @id";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@surname", SurnameTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@name", NameTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@patronymic",
                                string.IsNullOrWhiteSpace(PatronymicTextBox.Text) ? DBNull.Value : (object)PatronymicTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@phone", PhoneTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@contract", ContractNumberTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@specialization",
                                string.IsNullOrWhiteSpace(SpecializationTextBox.Text) ? DBNull.Value : (object)SpecializationTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@id", _editingMaster.MasterId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Добавление
                        string sql = @"INSERT INTO masters (master_surname, master_name, master_patronymic, 
                                        master_phone_number, master_contract_number, master_specialization) 
                                      VALUES (@surname, @name, @patronymic, @phone, @contract, @specialization)";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@surname", SurnameTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@name", NameTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@patronymic",
                                string.IsNullOrWhiteSpace(PatronymicTextBox.Text) ? DBNull.Value : (object)PatronymicTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@phone", PhoneTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@contract", ContractNumberTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@specialization",
                                string.IsNullOrWhiteSpace(SpecializationTextBox.Text) ? DBNull.Value : (object)SpecializationTextBox.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}