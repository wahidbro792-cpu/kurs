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
using Word = Microsoft.Office.Interop.Word;
using System.Diagnostics;


namespace AtlasServiceCenter.Pages
{
    public partial class ReportsPage : UserControl
    {
        public ReportsPage()
        {
            InitializeComponent();
            InitDefaultPeriod();
            LoadReport();
        }

        private void InitDefaultPeriod()
        {
            var today = DateTime.Today;
            var start = new DateTime(today.Year, today.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);

            FromDatePicker.SelectedDate = start;
            ToDatePicker.SelectedDate = end;
            OnlyPaidCheckBox.IsChecked = true;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadReport();
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            LoadReport();
        }

        private void LoadReport()
        {
            if (!FromDatePicker.SelectedDate.HasValue || !ToDatePicker.SelectedDate.HasValue)
                return;

            DateTime dateFrom = FromDatePicker.SelectedDate.Value.Date;
            DateTime dateTo = ToDatePicker.SelectedDate.Value.Date.AddDays(1); // включительно
            bool onlyPaid = OnlyPaidCheckBox.IsChecked == true;

            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var query =
                        from o in db.RepairOrders
                        join c in db.Clients on o.ClientId equals c.ClientId
                        join d in db.Devices on o.DeviceId equals d.DeviceId
                        join s in db.OrderStatuses on o.StatusId equals s.StatusId
                        where o.CreatedAt >= dateFrom && o.CreatedAt < dateTo
                        select new
                        {
                            o.OrderId,
                            o.CreatedAt,
                            ClientName = c.FullName,
                            DeviceName = d.DeviceType + " " + (d.Brand ?? "") + " " + (d.Model ?? ""),
                            StatusName = s.Name,
                            o.TotalAmount,
                            o.IsPaid,
                            o.IsIssued
                        };

                    if (onlyPaid)
                        query = query.Where(x => x.IsPaid == true);

                    var list = query
                        .AsEnumerable()
                        .Select(x => new ReportRow
                        {
                            OrderId = x.OrderId,
                            CreatedAt = x.CreatedAt,
                            ClientName = x.ClientName,
                            DeviceName = x.DeviceName.Trim(),
                            StatusName = x.StatusName,
                            TotalAmount = x.TotalAmount,
                            IsPaid = x.IsPaid,
                            IsIssued = x.IsIssued
                        })
                        .OrderBy(x => x.CreatedAt)
                        .ToList();

                    OrdersGrid.ItemsSource = list;

                    int totalOrders = list.Count;
                    int closedOrders = list.Count(r => r.IsIssued);
                    decimal revenue = list.Where(r => r.IsPaid).Sum(r => r.TotalAmount);

                    TotalOrdersText.Text = totalOrders.ToString();
                    ClosedOrdersText.Text = closedOrders.ToString();
                    RevenueText.Text = revenue.ToString("0.00", CultureInfo.InvariantCulture);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки отчёта:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === ВСПОМОГАТЕЛЬНОЕ: путь к папке отчётов ===

        private string GetReportsFolder()
        {
            string appBase = AppDomain.CurrentDomain.BaseDirectory;
            string folder = System.IO.Path.Combine(appBase, "Reports");
            return folder;
        }

        // === Экспорт в Word ===

        private void ExportToWordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rows = OrdersGrid.ItemsSource as IEnumerable<ReportRow>;
                if (rows == null || !rows.Any())
                {
                    MessageBox.Show("Нет данных для экспорта.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!FromDatePicker.SelectedDate.HasValue || !ToDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Укажите период отчёта.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DateTime dateFrom = FromDatePicker.SelectedDate.Value.Date;
                DateTime dateTo = ToDatePicker.SelectedDate.Value.Date;

                ExportReportToWord(rows.ToList(), dateFrom, dateTo,
                    TotalOrdersText.Text,
                    ClosedOrdersText.Text,
                    RevenueText.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта в Word:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportReportToWord(
            IList<ReportRow> rows,
            DateTime dateFrom,
            DateTime dateTo,
            string totalOrdersText,
            string closedOrdersText,
            string revenueText)
        {
            // Папка внутри приложения: bin\Debug\Reports (или bin\Release\Reports)
            string folder = GetReportsFolder();

            if (!System.IO.Directory.Exists(folder))
                System.IO.Directory.CreateDirectory(folder);

            string fileName = string.Format("Отчёт_{0:yyyyMMdd_HHmmss}.docx", DateTime.Now);
            string filePath = System.IO.Path.Combine(folder, fileName);

            Word.Application wordApp = null;
            Word.Document doc = null;

            try
            {
                wordApp = new Word.Application();
                wordApp.Visible = false;

                doc = wordApp.Documents.Add();

                // Заголовок
                Word.Paragraph pTitle = doc.Paragraphs.Add();
                pTitle.Range.Text = "Отчёт по заказам";
                pTitle.Range.Font.Bold = 1;
                pTitle.Range.Font.Size = 16;
                pTitle.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                pTitle.Range.InsertParagraphAfter();

                // Период
                Word.Paragraph pPeriod = doc.Paragraphs.Add();
                pPeriod.Range.Text = string.Format("Период: {0:dd.MM.yyyy} — {1:dd.MM.yyyy}",
                    dateFrom, dateTo);
                pPeriod.Range.Font.Bold = 0;
                pPeriod.Range.Font.Size = 11;
                pPeriod.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                pPeriod.Range.InsertParagraphAfter();

                // Итоги
                Word.Paragraph pTotals = doc.Paragraphs.Add();
                pTotals.Range.Text = string.Format(
                    "Всего заказов: {0}; закрыто/выдано: {1}; выручка (оплаченные): {2}",
                    totalOrdersText,
                    closedOrdersText,
                    revenueText);
                pTotals.Range.Font.Size = 11;
                pTotals.Range.InsertParagraphAfter();

                // Таблица
                int rowsCount = rows.Count;
                int colsCount = 7; // ID, Дата, Клиент, Устройство, Статус, Сумма, Оплачен

                Word.Paragraph pTable = doc.Paragraphs.Add();
                Word.Table table = doc.Tables.Add(pTable.Range, rowsCount + 1, colsCount);
                table.Borders.Enable = 1;

                int r = 1;

                table.Cell(r, 1).Range.Text = "ID";
                table.Cell(r, 2).Range.Text = "Дата";
                table.Cell(r, 3).Range.Text = "Клиент";
                table.Cell(r, 4).Range.Text = "Устройство";
                table.Cell(r, 5).Range.Text = "Статус";
                table.Cell(r, 6).Range.Text = "Сумма";
                table.Cell(r, 7).Range.Text = "Оплачен";

                table.Rows[r].Range.Bold = 1;

                foreach (var row in rows)
                {
                    r++;
                    table.Cell(r, 1).Range.Text = row.OrderId.ToString();
                    table.Cell(r, 2).Range.Text = row.CreatedAt.ToString("dd.MM.yyyy");
                    table.Cell(r, 3).Range.Text = row.ClientName;
                    table.Cell(r, 4).Range.Text = row.DeviceName;
                    table.Cell(r, 5).Range.Text = row.StatusName;
                    table.Cell(r, 6).Range.Text = row.TotalAmount.ToString("0.00");
                    table.Cell(r, 7).Range.Text = row.IsPaid ? "Да" : "Нет";
                }

                // Сохраняем и показываем Word с этим документом
                doc.SaveAs2(filePath);
                wordApp.Visible = true;

                MessageBox.Show("Отчёт сохранён в папку приложения и открыт в Word:\n" + filePath,
                    "Экспорт завершён", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                // Word и документ оставляем открытыми у пользователя
            }
        }

        // === Открыть папку с отчётами ===

        private void OpenReportsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string folder = GetReportsFolder();

                if (!System.IO.Directory.Exists(folder))
                {
                    MessageBox.Show("Папка с отчётами ещё не создана. Сначала экспортируйте хотя бы один отчёт.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Process.Start("explorer.exe", folder);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть папку с отчётами:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private class ReportRow
        {
            public int OrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public string ClientName { get; set; }
            public string DeviceName { get; set; }
            public string StatusName { get; set; }
            public decimal TotalAmount { get; set; }
            public bool IsPaid { get; set; }
            public bool IsIssued { get; set; }
        }
    }
}
