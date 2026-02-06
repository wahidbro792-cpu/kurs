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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using AtlasServiceCenter.Windows;

namespace AtlasServiceCenter.Pages
{
    public partial class PartsPage : UserControl
    {
        private ObservableCollection<PartRow> _allParts;

        public PartsPage()
        {
            InitializeComponent();
            LoadParts();
        }

        private void LoadParts()
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var list = db.Parts
                                 .OrderBy(p => p.Name)
                                 .Select(p => new PartRow
                                 {
                                     PartId = p.PartId,
                                     Name = p.Name,
                                     SKU = p.SKU,
                                     QuantityInStock = p.QuantityInStock,
                                     PurchasePrice = p.PurchasePrice,
                                     SalePrice = p.SalePrice,
                                     IsActive = p.IsActive
                                 })
                                 .ToList();

                    _allParts = new ObservableCollection<PartRow>(list);
                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки запчастей:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            if (_allParts == null)
                return;

            string search = (SearchBox.Text ?? string.Empty).Trim().ToLower();
            bool activeOnly = ActiveOnlyCheckBox.IsChecked == true;

            IEnumerable<PartRow> data = _allParts;

            if (activeOnly)
                data = data.Where(p => p.IsActive);

            if (!string.IsNullOrEmpty(search))
            {
                data = data.Where(p =>
                    (!string.IsNullOrEmpty(p.Name) && p.Name.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(p.SKU) && p.SKU.ToLower().Contains(search)));
            }

            PartsGrid.ItemsSource = data.ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ActiveOnlyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            ActiveOnlyCheckBox.IsChecked = true;
            LoadParts();
        }

        private PartRow GetSelectedPart()
        {
            return PartsGrid.SelectedItem as PartRow;
        }

        private void NewPartButton_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new PartCardWindow(null);
            wnd.Owner = Window.GetWindow(this);
            if (wnd.ShowDialog() == true)
            {
                LoadParts();
            }
        }

        private void EditPartButton_Click(object sender, RoutedEventArgs e)
        {
            var row = GetSelectedPart();
            if (row == null)
            {
                MessageBox.Show("Выберите запчасть для редактирования.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var wnd = new PartCardWindow(row.PartId);
            wnd.Owner = Window.GetWindow(this);
            if (wnd.ShowDialog() == true)
            {
                LoadParts();
            }
        }

        private void PartsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var row = GetSelectedPart();
            if (row == null)
                return;

            var wnd = new PartCardWindow(row.PartId);
            wnd.Owner = Window.GetWindow(this);
            if (wnd.ShowDialog() == true)
            {
                LoadParts();
            }
        }

        public class PartRow
        {
            public int PartId { get; set; }
            public string Name { get; set; }
            public string SKU { get; set; }
            public int QuantityInStock { get; set; }
            public decimal PurchasePrice { get; set; }
            public decimal SalePrice { get; set; }
            public bool IsActive { get; set; }
        }
    }
}

