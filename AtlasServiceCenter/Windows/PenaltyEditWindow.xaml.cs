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
    public partial class PenaltyEditWindow : Window
    {
        private readonly int _employeeId;

        public PenaltyEditWindow(int employeeId, string employeeFullName)
        {
            InitializeComponent();

            _employeeId = employeeId;
            EmployeeNameText.Text = employeeFullName ?? string.Empty;

            PenaltyDatePicker.SelectedDate = DateTime.Today;
            AmountBox.Text = "0.00";
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!PenaltyDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату штрафа.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal amount;
            if (!decimal.TryParse(AmountBox.Text.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out amount) || amount <= 0)
            {
                MessageBox.Show("Некорректная сумма штрафа.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string reason = (ReasonBox.Text ?? string.Empty).Trim();

            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var penalty = new Penalties
                    {
                        EmployeeId = _employeeId,
                        PenaltyDate = PenaltyDatePicker.SelectedDate.Value.Date,
                        Amount = amount,
                        Reason = reason
                    };

                    db.Penalties.Add(penalty);
                    db.SaveChanges();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения штрафа:\n" + ex.Message,
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
