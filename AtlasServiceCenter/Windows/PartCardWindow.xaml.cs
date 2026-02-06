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
    public partial class PartCardWindow : Window
    {
        private readonly int? _partId;

        public PartCardWindow(int? partId)
        {
            InitializeComponent();
            _partId = partId;
            LoadPart();
        }

        private void LoadPart()
        {
            if (!_partId.HasValue)
            {
                Title = "Новая запчасть";
                QuantityBox.Text = "0";
                PurchasePriceBox.Text = "0.00";
                SalePriceBox.Text = "0.00";
                IsActiveCheckBox.IsChecked = true;
                return;
            }

            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var part = db.Parts.FirstOrDefault(p => p.PartId == _partId.Value);
                    if (part == null)
                    {
                        MessageBox.Show("Запчасть не найдена.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    Title = "Запчасть: " + part.Name;

                    NameBox.Text = part.Name;
                    SkuBox.Text = part.SKU;
                    QuantityBox.Text = part.QuantityInStock.ToString(CultureInfo.InvariantCulture);
                    PurchasePriceBox.Text = part.PurchasePrice.ToString("0.00", CultureInfo.InvariantCulture);
                    SalePriceBox.Text = part.SalePrice.ToString("0.00", CultureInfo.InvariantCulture);
                    IsActiveCheckBox.IsChecked = part.IsActive;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки запчасти:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = (NameBox.Text ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Введите название запчасти.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int quantity;
                if (!int.TryParse(QuantityBox.Text, out quantity) || quantity < 0)
                {
                    MessageBox.Show("Некорректное количество.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal purchasePrice;
                if (!decimal.TryParse(PurchasePriceBox.Text.Replace(',', '.'),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out purchasePrice) || purchasePrice < 0)
                {
                    MessageBox.Show("Некорректная закупочная цена.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal salePrice;
                if (!decimal.TryParse(SalePriceBox.Text.Replace(',', '.'),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out salePrice) || salePrice < 0)
                {
                    MessageBox.Show("Некорректная продажная цена.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var db = new AtlasServiceCenterEntities())
                {
                    Parts part;

                    if (_partId.HasValue)
                    {
                        part = db.Parts.FirstOrDefault(p => p.PartId == _partId.Value);
                        if (part == null)
                        {
                            MessageBox.Show("Запчасть не найдена при сохранении.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        part = new Parts();
                        db.Parts.Add(part);
                    }

                    part.Name = name;
                    part.SKU = string.IsNullOrWhiteSpace(SkuBox.Text) ? null : SkuBox.Text.Trim();
                    part.QuantityInStock = quantity;
                    part.PurchasePrice = purchasePrice;
                    part.SalePrice = salePrice;
                    part.IsActive = IsActiveCheckBox.IsChecked == true;

                    db.SaveChanges();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения запчасти:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

