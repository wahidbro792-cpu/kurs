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
    public partial class EmployeeWindow : Window
    {
        private readonly int? _employeeId;

        public EmployeeWindow(int? employeeId)
        {
            InitializeComponent();
            _employeeId = employeeId;

            LoadPositions();
            LoadEmployee();
        }

        private void LoadPositions()
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var positions = db.Positions
                                      .OrderBy(pos => pos.Name)
                                      .ToList();
                    PositionComboBox.ItemsSource = positions;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки должностей:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEmployee()
        {
            if (!_employeeId.HasValue)
            {
                Title = "Новый сотрудник";
                HireDatePicker.SelectedDate = DateTime.Today;
                IsActiveCheckBox.IsChecked = true;
                BaseSalaryBox.Text = "0.00";
                return;
            }

            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var empDb = db.Employees.FirstOrDefault(emp => emp.EmployeeId == _employeeId.Value);
                    if (empDb == null)
                    {
                        MessageBox.Show("Сотрудник не найден.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    Title = "Сотрудник: " + empDb.FullName;

                    FullNameBox.Text = empDb.FullName;
                    PositionComboBox.SelectedValue = empDb.PositionId;
                    HireDatePicker.SelectedDate = empDb.HireDate;
                    FireDatePicker.SelectedDate = empDb.FireDate;
                    BaseSalaryBox.Text = empDb.BaseSalary.ToString("0.00", CultureInfo.InvariantCulture);
                    IsActiveCheckBox.IsChecked = empDb.IsActive;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки сотрудника:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string fullName = (FullNameBox.Text ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(fullName))
                {
                    MessageBox.Show("Введите ФИО сотрудника.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PositionComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Выберите должность.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime? hireDate = HireDatePicker.SelectedDate;
                if (!hireDate.HasValue)
                {
                    MessageBox.Show("Выберите дату приёма на работу.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal baseSalary;
                if (!decimal.TryParse(BaseSalaryBox.Text.Replace(',', '.'),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out baseSalary) || baseSalary < 0)
                {
                    MessageBox.Show("Некорректный базовый оклад.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var db = new AtlasServiceCenterEntities())
                {
                    Employees emp;

                    if (_employeeId.HasValue)
                    {
                        emp = db.Employees.FirstOrDefault(empDb => empDb.EmployeeId == _employeeId.Value);
                        if (emp == null)
                        {
                            MessageBox.Show("Сотрудник не найден при сохранении.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        emp = new Employees();
                        db.Employees.Add(emp);
                    }

                    emp.FullName = fullName;
                    emp.PositionId = (int)PositionComboBox.SelectedValue;
                    emp.HireDate = hireDate.Value;
                    emp.FireDate = FireDatePicker.SelectedDate;
                    emp.BaseSalary = baseSalary;
                    emp.IsActive = IsActiveCheckBox.IsChecked == true;

                    db.SaveChanges();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения сотрудника:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
