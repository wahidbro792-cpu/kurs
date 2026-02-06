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
    public partial class BonusEditWindow : Window
    {
        private readonly int _employeeId;

        public BonusEditWindow(int employeeId, string employeeFullName)
        {
            InitializeComponent();

            _employeeId = employeeId;
            EmployeeNameText.Text = employeeFullName ?? string.Empty;

            BonusDatePicker.SelectedDate = DateTime.Today;
            AmountBox.Text = "0.00";
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!BonusDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату премии.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal amount;
            if (!decimal.TryParse(AmountBox.Text.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out amount) || amount <= 0)
            {
                MessageBox.Show("Некорректная сумма премии.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string reason = (ReasonBox.Text ?? string.Empty).Trim();

            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var bonus = new Bonuses
                    {
                        EmployeeId = _employeeId,
                        BonusDate = BonusDatePicker.SelectedDate.Value.Date,
                        Amount = amount,
                        Reason = reason
                    };

                    db.Bonuses.Add(bonus);
                    db.SaveChanges();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения премии:\n" + ex.Message,
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

