using System;
using System.Collections.Generic;
using System.Windows;
using LashAccountingSystem.Models;

namespace LashAccountingSystem
{
    public partial class AddServiceMaterialTempWindow : Window
    {
        public ServiceMaterial SelectedMaterial { get; private set; }
        private List<ServiceMaterial> _existingMaterials;
        private int _tempId;

        public AddServiceMaterialTempWindow(List<ServiceMaterial> existingMaterials, int tempId)
        {
            InitializeComponent();
            _existingMaterials = existingMaterials;
            _tempId = tempId;
            LoadMaterialsComboBox();
        }

        private void LoadMaterialsComboBox()
        {
            var materials = App.Current.Resources["MaterialsList"] as List<dynamic>;
            if (materials != null)
            {
                MaterialComboBox.ItemsSource = materials;
                MaterialComboBox.SelectedValuePath = "MaterialId";
                MaterialComboBox.DisplayMemberPath = "DisplayName";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (MaterialComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите материал!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(QuantityTextBox.Text, out decimal quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество (положительное число)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int materialId = (int)MaterialComboBox.SelectedValue;
            string materialName = MaterialComboBox.Text;

            foreach (var existing in _existingMaterials)
            {
                if (existing.MaterialId == materialId)
                {
                    MessageBox.Show("Этот материал уже добавлен для данной услуги!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            SelectedMaterial = new ServiceMaterial
            {
                TempId = _tempId,
                MaterialId = materialId,
                MaterialName = materialName,
                QuantityRequired = quantity
            };

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}