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
    public partial class CashierPage : UserControl
    {
        private readonly Random _random = new Random();

        public CashierPage()
        {
            InitializeComponent();

            Loaded += CashierPage_Loaded;
        }

        private void CashierPage_Loaded(object sender, RoutedEventArgs e)
        {
            InitFilters();
            LoadOrders();
            LoadReceiptsList();
        }

        private void InitFilters()
        {
            DateFilterPicker.SelectedDate = DateTime.Today;
            TodayOnlyCheckBox.IsChecked = true;
        }

        #region Загрузка заказов

        private void TodayOnlyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        private void DateFilterPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TodayOnlyCheckBox.IsChecked == true)
                LoadOrders();
        }

        private void LoadOrders()
        {
            if (OrdersGrid == null)
                return;

            DateTime? selectedDate = DateFilterPicker.SelectedDate;

            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var query =
                        from o in db.RepairOrders
                        join c in db.Clients on o.ClientId equals c.ClientId
                        join s in db.OrderStatuses on o.StatusId equals s.StatusId
                        where o.IsPaid == false
                        select new CashierOrderRow
                        {
                            OrderId = o.OrderId,
                            CreatedAt = o.CreatedAt,
                            ClientName = c.FullName,
                            TotalAmount = o.TotalAmount,
                            StatusName = s.Name
                        };

                    if (TodayOnlyCheckBox.IsChecked == true && selectedDate.HasValue)
                    {
                        var day = selectedDate.Value.Date;
                        var next = day.AddDays(1);

                        query = query.Where(x => x.CreatedAt >= day && x.CreatedAt < next);
                    }

                    var list = query
                        .OrderBy(x => x.CreatedAt)
                        .ToList();

                    OrdersGrid.ItemsSource = list;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки заказов для кассира:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            SelectedOrderInfoText.Text = "";
            ReceiptTextBox.Text = "";
        }

        #endregion

        #region Вспомогательные модели

        private class CashierOrderRow
        {
            public int OrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public string ClientName { get; set; }
            public decimal TotalAmount { get; set; }
            public string StatusName { get; set; }
        }

        private class OrderItemInfo
        {
            public string Name { get; set; }
            public decimal Qty { get; set; }
            public decimal Price { get; set; }
            public decimal LineTotal { get; set; }
        }

        private List<OrderItemInfo> GetOrderItems(int orderId)
        {
            using (var db = new AtlasServiceCenterEntities())
            {
                var worksQuery =
                    from w in db.RepairOrderWorks
                    where w.OrderId == orderId
                    select new OrderItemInfo
                    {
                        Name = w.WorkName,
                        Qty = w.Quantity,
                        Price = w.Price,
                        LineTotal = w.LineTotal
                    };

                var partsQuery =
                    from p in db.RepairOrderParts
                    where p.OrderId == orderId
                    select new OrderItemInfo
                    {
                        Name = p.Parts.Name,
                        Qty = p.Quantity,
                        Price = p.PricePerUnit,
                        LineTotal = p.LineTotal
                    };

                var result = worksQuery.ToList();
                result.AddRange(partsQuery.ToList());
                return result;
            }
        }

        #endregion

        #region Выбор заказа и генерация чека

        private CashierOrderRow GetSelectedOrder()
        {
            return OrdersGrid?.SelectedItem as CashierOrderRow;
        }

        private void OrdersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            GenerateReceiptForSelectedOrder();
        }

        private void GenerateReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateReceiptForSelectedOrder();
        }

        private void GenerateReceiptForSelectedOrder()
        {
            var order = GetSelectedOrder();
            if (order == null)
            {
                MessageBox.Show("Выберите заказ в списке.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string paymentType = (PaymentTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString()
                                 ?? "Наличные";

            int receiptNumber = _random.Next(100000, 999999);
            DateTime now = DateTime.Now;

            List<OrderItemInfo> items;
            try
            {
                items = GetOrderItems(order.OrderId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки позиций заказа:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            decimal itemsTotal = items?.Sum(i => i.LineTotal) ?? 0m;

            SelectedOrderInfoText.Text =
                $"ID {order.OrderId}, клиент: {order.ClientName}, сумма: {order.TotalAmount:0.00}";

            var sb = new StringBuilder();

            sb.AppendLine("          АТЛАС СЕРВИС ЦЕНТР");
            sb.AppendLine("      ИП Иванов Иван Иванович");
            sb.AppendLine("   ИНН 1234567890, г. Нижний Новгород");
            sb.AppendLine("           тел. +7 (999) 000-00-00");
            sb.AppendLine();
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"           КАССОВЫЙ ЧЕК № {receiptNumber}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"Дата/время: {now:dd.MM.yyyy HH:mm}");
            sb.AppendLine($"Заказ ID: {order.OrderId}");
            sb.AppendLine($"Клиент: {order.ClientName}");
            sb.AppendLine($"Статус заказа: {order.StatusName}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine("Позиции:");
            sb.AppendLine("Наименование               Кол-во     Цена      Сумма");
            sb.AppendLine("------------------------------------------------------");

            if (items != null && items.Count > 0)
            {
                foreach (var it in items)
                {
                    string name = it.Name ?? "";
                    if (name.Length > 26)
                        name = name.Substring(0, 26) + ".";

                    // имя слева, остальные колонки выровнены вправо
                    sb.AppendLine(
                        $"{name,-27}{it.Qty,8:0.##}{it.Price,10:0.00}{it.LineTotal,11:0.00}");
                }
            }
            else
            {
                sb.AppendLine("   (нет позиций)");
            }

            sb.AppendLine("------------------------------------------------------");
            sb.AppendLine($"Сумма по позициям: {itemsTotal:0.00} руб.");
            sb.AppendLine($"Сумма к оплате:    {order.TotalAmount:0.00} руб.");
            sb.AppendLine($"Способ оплаты:     {paymentType}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine("Спасибо за обращение в Atlas Service Center!");
            sb.AppendLine("Сохраните чек до конца гарантийного срока.");

            ReceiptTextBox.Text = sb.ToString();
        }

        #endregion

        #region Построение FlowDocument для печати

        private FlowDocument BuildReceiptDocument(string text)
        {
            var doc = new FlowDocument
            {
                PageWidth = 280,
                PagePadding = new Thickness(10),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                ColumnWidth = double.PositiveInfinity
            };

            var lines = text.Replace("\r\n", "\n").Split('\n');

            foreach (var rawLine in lines)
            {
                string line = rawLine;
                var p = new Paragraph(new Run(line))
                {
                    Margin = new Thickness(0, 0, 0, 0)
                };

                string trimmed = line.Trim();

                if (trimmed.StartsWith("АТЛАС СЕРВИС ЦЕНТР", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("ИП ", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("ИНН", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("тел.", StringComparison.OrdinalIgnoreCase))
                {
                    p.TextAlignment = TextAlignment.Center;
                    if (trimmed.StartsWith("АТЛАС СЕРВИС ЦЕНТР", StringComparison.OrdinalIgnoreCase))
                        p.FontWeight = FontWeights.Bold;
                }
                else if (trimmed.StartsWith("КАССОВЫЙ ЧЕК", StringComparison.OrdinalIgnoreCase))
                {
                    p.TextAlignment = TextAlignment.Center;
                    p.FontWeight = FontWeights.Bold;
                }
                else if (trimmed.StartsWith("Сумма к оплате", StringComparison.OrdinalIgnoreCase) ||
                         trimmed.StartsWith("Способ оплаты", StringComparison.OrdinalIgnoreCase))
                {
                    p.FontWeight = FontWeights.Bold;
                }

                doc.Blocks.Add(p);
            }

            return doc;
        }

        #endregion

        #region Печать и сохранение чека

        private void PrintReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ReceiptTextBox.Text))
            {
                MessageBox.Show("Сначала сгенерируйте чек.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var order = GetSelectedOrder();
            if (order == null)
            {
                MessageBox.Show("Не выбран заказ для оплаты.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                FlowDocument doc = BuildReceiptDocument(ReceiptTextBox.Text);

                PrintDialog dlg = new PrintDialog();
                if (dlg.ShowDialog() == true)
                {
                    doc.PageWidth = dlg.PrintableAreaWidth;
                    doc.PageHeight = dlg.PrintableAreaHeight;

                    dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Чек");
                }

                SaveReceiptToFile(order.OrderId, ReceiptTextBox.Text);
                MarkOrderAsPaid(order.OrderId);

                LoadOrders();
                LoadReceiptsList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка печати чека:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetReceiptsFolder()
        {
            string appBase = AppDomain.CurrentDomain.BaseDirectory;
            string folder = System.IO.Path.Combine(appBase, "Receipts");
            return folder;
        }

        private void SaveReceiptToFile(int orderId, string text)
        {
            try
            {
                string folder = GetReceiptsFolder();

                if (!System.IO.Directory.Exists(folder))
                    System.IO.Directory.CreateDirectory(folder);

                string fileName = string.Format("Receipt_{0}_{1:yyyyMMdd_HHmmss}.txt",
                    orderId, DateTime.Now);
                string filePath = System.IO.Path.Combine(folder, fileName);

                System.IO.File.WriteAllText(filePath, text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить чек в файл:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MarkOrderAsPaid(int orderId)
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var order = db.RepairOrders.FirstOrDefault(o => o.OrderId == orderId);
                    if (order != null)
                    {
                        order.IsPaid = true;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось пометить заказ как оплаченный:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region История чеков (оплаченные)

        private void LoadReceiptsList()
        {
            try
            {
                string folder = GetReceiptsFolder();
                if (!System.IO.Directory.Exists(folder))
                {
                    ReceiptsListBox.ItemsSource = null;
                    return;
                }

                var files = System.IO.Directory.GetFiles(folder, "Receipt_*.txt");

                var list = files
                    .Select(f => new ReceiptInfo
                    {
                        FilePath = f,
                        DisplayName = System.IO.Path.GetFileNameWithoutExtension(f)
                    })
                    .OrderByDescending(r => r.FilePath)
                    .ToList();

                ReceiptsListBox.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить список чеков:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReceiptsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var info = ReceiptsListBox.SelectedItem as ReceiptInfo;
            if (info == null)
                return;

            try
            {
                string text = System.IO.File.ReadAllText(info.FilePath);
                ReceiptTextBox.Text = text;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть чек:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private class ReceiptInfo
        {
            public string FilePath { get; set; }
            public string DisplayName { get; set; }

            public override string ToString() => DisplayName;
        }

        #endregion
    }
}


