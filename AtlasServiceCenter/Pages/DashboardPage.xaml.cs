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


namespace AtlasServiceCenter.Pages
{
    public partial class DashboardPage : UserControl
    {
        public DashboardPage()
        {
            InitializeComponent();
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var today = DateTime.Today;
                    var tomorrow = today.AddDays(1);

                    // 1. Активные заказы (ещё не выданы клиенту)
                    int activeOrders = db.RepairOrders
                        .Count(o => o.IsIssued == false);

                    // 2. Заказы, созданные сегодня
                    int todayOrders = db.RepairOrders
                        .Count(o => o.CreatedAt >= today && o.CreatedAt < tomorrow);

                    // 3. Выручка за сегодня по оплаченных заказам
                    decimal todayRevenue = db.RepairOrders
                        .Where(o => o.IsPaid == true &&
                                    o.CreatedAt >= today &&
                                    o.CreatedAt < tomorrow)
                        .Select(o => (decimal?)o.TotalAmount)
                        .Sum() ?? 0m;

                    ActiveOrdersText.Text = activeOrders.ToString();
                    TodayOrdersText.Text = todayOrders.ToString();
                    TodayRevenueText.Text = todayRevenue.ToString("0.00");

                    // 4. Сводка по статусам
                    var statusSummary = (from o in db.RepairOrders
                                         join s in db.OrderStatuses
                                             on o.StatusId equals s.StatusId
                                         group o by s.Name
                        into g
                                         select new StatusSummaryRow
                                         {
                                             StatusName = g.Key,
                                             Count = g.Count()
                                         })
                        .OrderBy(r => r.StatusName)
                        .ToList();

                    StatusSummaryGrid.ItemsSource = statusSummary;

                    // 5. Топ мастеров: выданные заказы за последние 30 дней
                    DateTime fromDate = today.AddDays(-30);

                    var topMasters = (from o in db.RepairOrders
                                      join e in db.Employees
                                          on o.MasterId equals (int?)e.EmployeeId
                                      where o.IsIssued == true
                                            && o.CreatedAt >= fromDate
                                            && o.CreatedAt < tomorrow
                                      group o by e.FullName
                        into g
                                      select new TopMasterRow
                                      {
                                          MasterName = g.Key,
                                          ClosedCount = g.Count()
                                      })
                        .OrderByDescending(r => r.ClosedCount)
                        .ToList();

                    TopMastersGrid.ItemsSource = topMasters;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки дашборда:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private class StatusSummaryRow
        {
            public string StatusName { get; set; }
            public int Count { get; set; }
        }

        private class TopMasterRow
        {
            public string MasterName { get; set; }
            public int ClosedCount { get; set; }
        }
    }
}
