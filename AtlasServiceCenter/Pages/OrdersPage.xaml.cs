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
using UserEntity = AtlasServiceCenter.Users;
using System.Collections.ObjectModel;
using AtlasServiceCenter.Windows;

namespace AtlasServiceCenter.Pages
{
    public partial class OrdersPage : UserControl
    {
        private readonly UserEntity _currentUser;

        // Полный список заказов, загруженный из БД
        private List<OrderRow> _allOrders;
        private bool _canCreateOrders;
        private bool _canDeleteOrders;
        private bool _canFilterByMaster;
        private bool _lockStatusFilter;
        private string[] _lockedStatusOptions;

        public OrdersPage(UserEntity currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            ConfigureAccessByRole();
            LoadOrders();
        }

        #region Модель строки

        public class OrderRow
        {
            public int OrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public string ClientName { get; set; }
            public string DeviceName { get; set; }
            public string StatusName { get; set; }
            public string MasterName { get; set; }
            public decimal TotalAmount { get; set; }
            public bool IsPaid { get; set; }
            public bool IsIssued { get; set; }
        }

        #endregion

        #region Загрузка заказов + справочников фильтров

        private void LoadOrders()
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var roleName = _currentUser.Roles?.Name;
                    int? employeeId = _currentUser.EmployeeId;

                    var baseQuery = db.RepairOrders.AsQueryable();

                    // Мастер видит только свои заказы
                    if (RoleHelper.IsMaster(roleName) && employeeId.HasValue)
                    {
                        baseQuery = baseQuery.Where(o => o.MasterId == employeeId.Value);
                    }

                    if (RoleHelper.IsCashier(roleName))
                    {
                        var readyNames = new[] { "Готов", "К выдаче", "Готов к выдаче" };
                        var readyStatusIds = db.OrderStatuses
                            .Where(s => readyNames.Contains(s.Name))
                            .Select(s => s.StatusId)
                            .ToList();

                        if (readyStatusIds.Any())
                        {
                            baseQuery = baseQuery.Where(o => readyStatusIds.Contains(o.StatusId));
                        }
                    }

                    var data =
                        (from o in baseQuery
                         join c in db.Clients on o.ClientId equals c.ClientId
                         join d in db.Devices on o.DeviceId equals d.DeviceId
                         join s in db.OrderStatuses on o.StatusId equals s.StatusId
                         join m in db.Employees on o.MasterId equals m.EmployeeId into masters
                         from m in masters.DefaultIfEmpty()
                         orderby o.CreatedAt descending
                         select new OrderRow
                         {
                             OrderId = o.OrderId,
                             CreatedAt = o.CreatedAt,
                             ClientName = c.FullName,
                             DeviceName = d.DeviceType + " " +
                                          (d.Brand ?? "") + " " +
                                          (d.Model ?? ""),
                             StatusName = s.Name,
                             MasterName = m != null ? m.FullName : "",
                             TotalAmount = o.TotalAmount,
                             IsPaid = o.IsPaid,
                             IsIssued = o.IsIssued
                         }).ToList();

                    _allOrders = data;

                    // Сброс полей фильтра
                    SearchBox.Text = string.Empty;
                    DateFrom.SelectedDate = null;
                    DateTo.SelectedDate = null;

                    LoadFilterLookups();
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки заказов:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadFilterLookups()
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    // Статусы
                    var statuses = db.OrderStatuses
                        .OrderBy(s => s.OrderIndex)
                        .Select(s => s.Name)
                        .ToList();

                    statuses.Insert(0, "Все");
                    StatusFilter.ItemsSource = statuses;
                    StatusFilter.SelectedIndex = 0;

                    if (_lockStatusFilter && _lockedStatusOptions != null)
                    {
                        int lockedIndex = -1;
                        foreach (var statusOption in _lockedStatusOptions)
                        {
                            lockedIndex = statuses.IndexOf(statusOption);
                            if (lockedIndex >= 0)
                                break;
                        }

                        if (lockedIndex >= 0)
                            StatusFilter.SelectedIndex = lockedIndex;

                        StatusFilter.IsEnabled = false;
                        StatusFilterLabel.Foreground = Brushes.Gray;
                    }

                    // Мастера (по должности "Мастер")
                    var masters = (from e in db.Employees
                                   join p in db.Positions on e.PositionId equals p.PositionId
                                   where p.Name == "Мастер"
                                   orderby e.FullName
                                   select e.FullName)
                                   .ToList();

                    masters.Insert(0, "Все");
                    MasterFilter.ItemsSource = masters;
                    MasterFilter.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки справочников для фильтров:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Фильтрация

        private void ConfigureAccessByRole()
        {
            var roleName = _currentUser.Roles?.Name;

            _canCreateOrders = RoleHelper.CanCreateOrders(roleName);
            _canDeleteOrders = RoleHelper.CanDeleteOrders(roleName);
            _canFilterByMaster = RoleHelper.IsOwner(roleName) ||
                                 RoleHelper.IsAdmin(roleName) ||
                                 RoleHelper.IsReceptionManager(roleName);

            if (RoleHelper.IsCashier(roleName))
            {
                _lockStatusFilter = true;
                _lockedStatusOptions = new[] { "Готов", "К выдаче", "Готов к выдаче" };
            }

            ApplyAccessToControls();
        }

        private void ApplyAccessToControls()
        {
            NewOrderButton.IsEnabled = _canCreateOrders;
            DeleteOrderButton.IsEnabled = _canDeleteOrders;

            if (!_canFilterByMaster)
            {
                MasterFilter.Visibility = Visibility.Collapsed;
                MasterFilterLabel.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyFilters()
        {
            if (_allOrders == null)
                return;

            IEnumerable<OrderRow> list = _allOrders;

            // Поиск
            string search = (SearchBox.Text ?? string.Empty).Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                list = list.Where(o =>
                    (!string.IsNullOrEmpty(o.ClientName) &&
                     o.ClientName.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(o.DeviceName) &&
                     o.DeviceName.ToLower().Contains(search)));
            }

            // Фильтр по статусу
            if (StatusFilter.SelectedItem is string status && status != "Все")
            {
                list = list.Where(o => o.StatusName == status);
            }

            // Фильтр по мастеру
            if (MasterFilter.SelectedItem is string master && master != "Все")
            {
                list = list.Where(o => o.MasterName == master);
            }

            // Фильтр по дате "с"
            if (DateFrom.SelectedDate.HasValue)
            {
                var from = DateFrom.SelectedDate.Value.Date;
                list = list.Where(o => o.CreatedAt.Date >= from);
            }

            // Фильтр по дате "по"
            if (DateTo.SelectedDate.HasValue)
            {
                var to = DateTo.SelectedDate.Value.Date;
                list = list.Where(o => o.CreatedAt.Date <= to);
            }

            OrdersGrid.ItemsSource = list.ToList();
        }

        // Обработчики изменений фильтров

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void MasterFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DateFrom_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DateTo_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        #endregion

        #region Кнопки панели

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        private void NewOrderButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new OrderWindow(null, _currentUser)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
            LoadOrders();
        }

        private void OpenOrderButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedOrder();
        }

        private void DeleteOrderButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedOrder();
        }

        #endregion

        #region Открытие заказа

        private void OrdersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (OrdersGrid.SelectedItem != null)
                OpenSelectedOrder();
        }

        private void OpenSelectedOrder()
        {
            var row = OrdersGrid.SelectedItem as OrderRow;
            if (row == null)
            {
                MessageBox.Show("Выберите заказ.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new OrderWindow(row.OrderId, _currentUser)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
            LoadOrders();
        }

        #endregion

        #region Удаление заказов (с учётом ролей)

        // Удалять выполненные (оплаченные/выданные) заказы
        // могут только Владелец, Администратор, Менеджер
        private bool CanDeleteCompletedOrders()
        {
            var roleName = _currentUser.Roles?.Name;

            return RoleHelper.IsAdmin(roleName) || RoleHelper.IsReceptionManager(roleName);
        }

        private void DeleteSelectedOrder()
        {
            var row = OrdersGrid.SelectedItem as OrderRow;
            if (row == null)
            {
                MessageBox.Show("Выберите заказ для удаления.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var order = db.RepairOrders.FirstOrDefault(o => o.OrderId == row.OrderId);
                    if (order == null)
                    {
                        MessageBox.Show("Заказ не найден в базе.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    bool isCompleted = order.IsPaid || order.IsIssued;

                    if (isCompleted && !CanDeleteCompletedOrders())
                    {
                        MessageBox.Show(
                            "Удалять выполненные/оплаченные/выданные заказы " +
                            "может только Владелец, Администратор или Менеджер.",
                            "Отказ в доступе",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var result = MessageBox.Show(
                        $"Удалить заказ №{order.OrderId}?",
                        "Подтверждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                        return;

                    // Каскадные связи удалят RepairOrderWorks и RepairOrderParts
                    db.RepairOrders.Remove(order);
                    db.SaveChanges();
                }

                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления заказа:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
