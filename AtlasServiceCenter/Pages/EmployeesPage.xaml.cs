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
    public partial class EmployeesPage : UserControl
    {
        private readonly Users _currentUser;
        private ObservableCollection<EmployeeRow> _allEmployees;

        public EmployeesPage(Users currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            ConfigureAccessByRole();
            LoadEmployees();
        }

        private void ConfigureAccessByRole()
        {
            bool canManage = RoleHelper.CanManageEmployees(_currentUser.Roles?.Name);
            NewEmployeeButton.IsEnabled = canManage;
        }

        private void LoadEmployees()
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var query =
                        from e in db.Employees
                        join p in db.Positions on e.PositionId equals p.PositionId
                        orderby e.IsActive descending, e.FullName
                        select new EmployeeRow
                        {
                            EmployeeId = e.EmployeeId,
                            FullName = e.FullName,
                            PositionName = p.Name,
                            HireDate = e.HireDate,
                            FireDate = e.FireDate,
                            BaseSalary = e.BaseSalary,
                            IsActive = e.IsActive
                        };

                    var list = query.ToList();

                    _allEmployees = new ObservableCollection<EmployeeRow>(list);
                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки сотрудников:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            if (_allEmployees == null)
                return;

            string search = (SearchBox.Text ?? string.Empty).Trim().ToLower();
            bool activeOnly = ActiveOnlyCheckBox.IsChecked == true;

            IEnumerable<EmployeeRow> data = _allEmployees;

            if (activeOnly)
                data = data.Where(e => e.IsActive);

            if (!string.IsNullOrEmpty(search))
            {
                data = data.Where(e =>
                    !string.IsNullOrEmpty(e.FullName) &&
                    e.FullName.ToLower().Contains(search));
            }

            EmployeesGrid.ItemsSource = data.ToList();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            ActiveOnlyCheckBox.IsChecked = true;
            LoadEmployees();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ActiveOnlyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        private EmployeeRow GetSelectedEmployeeRow()
        {
            return EmployeesGrid.SelectedItem as EmployeeRow;
        }

        private void EmployeesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var row = GetSelectedEmployeeRow();
            if (row == null)
                return;

            if (!RoleHelper.CanManageEmployees(_currentUser.Roles?.Name))
            {
                MessageBox.Show("Просмотр карточек сотрудников доступен только владельцу.",
                    "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var wnd = new EmployeeWindow(row.EmployeeId);
            wnd.Owner = Window.GetWindow(this);
            wnd.ShowDialog();

            LoadEmployees();
        }

        private void NewEmployeeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!RoleHelper.CanManageEmployees(_currentUser.Roles?.Name))
            {
                MessageBox.Show("Добавлять сотрудников может только владелец.",
                    "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var wnd = new EmployeeWindow(null);
            wnd.Owner = Window.GetWindow(this);
            wnd.ShowDialog();

            LoadEmployees();
        }

        public class EmployeeRow
        {
            public int EmployeeId { get; set; }
            public string FullName { get; set; }
            public string PositionName { get; set; }
            public DateTime HireDate { get; set; }
            public DateTime? FireDate { get; set; }
            public decimal BaseSalary { get; set; }
            public bool IsActive { get; set; }
        }
    }
}

