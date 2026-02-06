using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Globalization;




namespace AtlasServiceCenter.Windows
{
    public partial class PartEditWindow : Window
    {
        public int SelectedPartId { get; private set; }
        public decimal PricePerUnit { get; private set; }
        public int Quantity { get; private set; }

        private List<PartOption> _options;
        private bool _isEditMode;

        public class PartOption
        {
            public int PartId { get; set; }
            public string DisplayName { get; set; }
            public decimal SalePrice { get; set; }
            public int QuantityInStock { get; set; }
        }

        public PartEditWindow(List<Parts> availableParts, RepairOrderParts existing)
        {
            InitializeComponent();

            if (availableParts == null)
                availableParts = new List<Parts>();

            _options = availableParts
                .Select(p => new PartOption
                {
                    PartId = p.PartId,
                    SalePrice = p.SalePrice,
                    QuantityInStock = p.QuantityInStock,
                    DisplayName = string.Format("{0} (ост.: {1})", p.Name, p.QuantityInStock)
                })
                .ToList();

            PartComboBox.ItemsSource = _options;
            PartComboBox.DisplayMemberPath = "DisplayName";
            PartComboBox.SelectedValuePath = "PartId";

            if (existing != null)
            {
                _isEditMode = true;
                Title = "Редактирование запчасти";

                PartComboBox.SelectedValue = existing.PartId;
                PartComboBox.IsEnabled = false;

                PriceBox.Text = existing.PricePerUnit.ToString("0.00", CultureInfo.InvariantCulture);
                QuantityBox.Text = existing.Quantity.ToString(CultureInfo.InvariantCulture);

                SelectedPartId = existing.PartId;
            }
            else
            {
                _isEditMode = false;
                Title = "Добавление запчасти";

                if (_options.Count > 0)
                {
                    PartComboBox.SelectedIndex = 0;
                    var first = _options[0];
                    PriceBox.Text = first.SalePrice.ToString("0.00", CultureInfo.InvariantCulture);
                }

                QuantityBox.Text = "1";
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var opt = PartComboBox.SelectedItem as PartOption;
            if (opt == null)
            {
                MessageBox.Show("Выберите запчасть.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal price;
            if (!decimal.TryParse(PriceBox.Text.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out price) || price < 0)
            {
                MessageBox.Show("Некорректная цена.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int qty;
            if (!int.TryParse(QuantityBox.Text, out qty) || qty <= 0)
            {
                MessageBox.Show("Некорректное количество.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // При добавлении новой запчасти сразу проверяем остаток.
            // При редактировании запас контролируется в OrderWindow.
            if (!_isEditMode && qty > opt.QuantityInStock)
            {
                MessageBox.Show(
                    string.Format("Недостаточно остатков на складе.\nДоступно: {0}", opt.QuantityInStock),
                    "Недостаточно на складе",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            SelectedPartId = opt.PartId;
            PricePerUnit = price;
            Quantity = qty;

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
