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
    public partial class WorkEditWindow : Window
    {
        public string WorkName { get; private set; }
        public decimal Price { get; private set; }
        public int Quantity { get; private set; }

        public WorkEditWindow(RepairOrderWorks existing, bool canEditPrice)
        {
            InitializeComponent();

            PriceBox.IsReadOnly = !canEditPrice;
            if (!canEditPrice)
            {
                PriceBox.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            }

            if (existing != null)
            {
                Title = "Редактирование работы";
                WorkNameBox.Text = existing.WorkName;
                PriceBox.Text = existing.Price.ToString("0.00", CultureInfo.InvariantCulture);
                QuantityBox.Text = existing.Quantity.ToString();
            }
            else
            {
                Title = "Добавление работы";
                QuantityBox.Text = "1";
                if (!canEditPrice)
                {
                    PriceBox.Text = "0.00";
                }
            }
        }

        public WorkEditWindow() : this(null, true)
        {
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            WorkName = WorkNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(WorkName))
            {
                MessageBox.Show("Введите наименование работы.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(PriceBox.Text.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price < 0)
            {
                MessageBox.Show("Некорректная цена.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityBox.Text, out var qty) || qty <= 0)
            {
                MessageBox.Show("Некорректное количество.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Price = price;
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
