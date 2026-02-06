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
using System.Globalization;
using AtlasServiceCenter.Windows;   // чтобы видеть окна BonusEditWindow / PenaltyEditWindow

namespace AtlasServiceCenter.Pages
{
    public partial class SalaryPage : UserControl
    {
        private readonly Users _currentUser;
        private List<Employees> _employees;

        public SalaryPage(Users currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            if (!RoleHelper.CanAccessSalary(_currentUser.Roles?.Name))
            {
                IsEnabled = false;
                MessageBox.Show("Доступ к зарплате есть только у владельца.",
                    "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadEmployees();
            InitDefaultPeriod();
            CalculateCurrent();
        }

        private void LoadEmployees()
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    _employees = db.Employees
                                   .OrderBy(emp => emp.FullName)
                                   .ToList();
                    EmployeeComboBox.ItemsSource = _employees;

                    if (_employees.Count > 0)
                        EmployeeComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки сотрудников:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitDefaultPeriod()
        {
            var today = DateTime.Today;
            var start = new DateTime(today.Year, today.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);

            PeriodStartPicker.SelectedDate = start;
            PeriodEndPicker.SelectedDate = end;
        }

        private void EmployeeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadPayments();
            CalculateCurrent();
        }

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            CalculateCurrent();
        }

        private void CalculateCurrent()
        {
            if (EmployeeComboBox.SelectedValue == null ||
                !PeriodStartPicker.SelectedDate.HasValue ||
                !PeriodEndPicker.SelectedDate.HasValue)
            {
                ClearCalculationFields();
                return;
            }

            int employeeId = (int)EmployeeComboBox.SelectedValue;
            DateTime start = PeriodStartPicker.SelectedDate.Value.Date;
            DateTime end = PeriodEndPicker.SelectedDate.Value.Date;

            if (end < start)
            {
                MessageBox.Show("Дата окончания периода не может быть раньше даты начала.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var emp = db.Employees.FirstOrDefault(em => em.EmployeeId == employeeId);
                    if (emp == null)
                    {
                        ClearCalculationFields();
                        return;
                    }

                    decimal baseSalary = emp.BaseSalary;

                    decimal bonusesTotal = db.Bonuses
                        .Where(b => b.EmployeeId == employeeId &&
                                    b.BonusDate >= start &&
                                    b.BonusDate <= end)
                        .Select(b => (decimal?)b.Amount)
                        .Sum() ?? 0m;

                    decimal penaltiesTotal = db.Penalties
                        .Where(p => p.EmployeeId == employeeId &&
                                    p.PenaltyDate >= start &&
                                    p.PenaltyDate <= end)
                        .Select(p => (decimal?)p.Amount)
                        .Sum() ?? 0m;

                    decimal finalAmount = baseSalary + bonusesTotal - penaltiesTotal;

                    BaseSalaryBox.Text = baseSalary.ToString("0.00", CultureInfo.InvariantCulture);
                    BonusesBox.Text = bonusesTotal.ToString("0.00", CultureInfo.InvariantCulture);
                    PenaltiesBox.Text = penaltiesTotal.ToString("0.00", CultureInfo.InvariantCulture);
                    FinalAmountBox.Text = finalAmount.ToString("0.00", CultureInfo.InvariantCulture);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка расчёта зарплаты:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                ClearCalculationFields();
            }
        }

        private void ClearCalculationFields()
        {
            BaseSalaryBox.Text = "";
            BonusesBox.Text = "";
            PenaltiesBox.Text = "";
            FinalAmountBox.Text = "";
        }

        private void LoadPayments()
        {
            if (EmployeeComboBox.SelectedValue == null)
            {
                PaymentsGrid.ItemsSource = null;
                return;
            }

            int employeeId = (int)EmployeeComboBox.SelectedValue;

            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var payments = db.SalaryPayments
                                     .Where(sp => sp.EmployeeId == employeeId)
                                     .OrderByDescending(sp => sp.PeriodEnd)
                                     .ToList();

                    PaymentsGrid.ItemsSource = payments;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки выплат:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeComboBox.SelectedValue == null ||
                !PeriodStartPicker.SelectedDate.HasValue ||
                !PeriodEndPicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите сотрудника и период.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal baseSalary;
            decimal bonusesTotal;
            decimal penaltiesTotal;
            decimal finalAmount;

            if (!decimal.TryParse(BaseSalaryBox.Text.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out baseSalary))
                baseSalary = 0m;

            if (!decimal.TryParse(BonusesBox.Text.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out bonusesTotal))
                bonusesTotal = 0m;

            if (!decimal.TryParse(PenaltiesBox.Text.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out penaltiesTotal))
                penaltiesTotal = 0m;

            if (!decimal.TryParse(FinalAmountBox.Text.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out finalAmount))
                finalAmount = 0m;

            int employeeId = (int)EmployeeComboBox.SelectedValue;
            DateTime start = PeriodStartPicker.SelectedDate.Value.Date;
            DateTime end = PeriodEndPicker.SelectedDate.Value.Date;
            string comment = (CommentBox.Text ?? string.Empty).Trim();

            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var payment = new SalaryPayments
                    {
                        EmployeeId = employeeId,
                        PeriodStart = start,
                        PeriodEnd = end,
                        BaseSalary = baseSalary,
                        BonusesTotal = bonusesTotal,
                        PenaltiesTotal = penaltiesTotal,
                        FinalAmount = finalAmount,
                        PaymentDate = DateTime.Today,
                        Comment = comment
                    };

                    db.SalaryPayments.Add(payment);
                    db.SaveChanges();
                }

                MessageBox.Show("Выплата сохранена.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                CommentBox.Text = string.Empty;
                LoadPayments();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения выплаты:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === Добавить премию ===
        private void AddBonusButton_Click(object sender, RoutedEventArgs e)
        {
            var emp = EmployeeComboBox.SelectedItem as Employees;
            if (emp == null)
            {
                MessageBox.Show("Сначала выберите сотрудника.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var wnd = new BonusEditWindow(emp.EmployeeId, emp.FullName);
            wnd.Owner = Window.GetWindow(this);

            if (wnd.ShowDialog() == true)
            {
                // После добавления премии — пересчитать и обновить
                CalculateCurrent();
            }
        }

        // === Добавить штраф ===
        private void AddPenaltyButton_Click(object sender, RoutedEventArgs e)
        {
            var emp = EmployeeComboBox.SelectedItem as Employees;
            if (emp == null)
            {
                MessageBox.Show("Сначала выберите сотрудника.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var wnd = new PenaltyEditWindow(emp.EmployeeId, emp.FullName);
            wnd.Owner = Window.GetWindow(this);

            if (wnd.ShowDialog() == true)
            {
                // После добавления штрафа — пересчитать
                CalculateCurrent();
            }
        }
    }
}
