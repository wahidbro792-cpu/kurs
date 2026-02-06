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
using UserEntity = AtlasServiceCenter.Users;
using System.Globalization;

namespace AtlasServiceCenter.Windows
{
    public partial class OrderWindow : Window
    {
        private int? _orderId;
        private readonly UserEntity _currentUser;
        private bool _canEditPrices;

        public OrderWindow(int? orderId, UserEntity currentUser)
        {
            InitializeComponent();
            _orderId = orderId;
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            LoadDictionaries();
            LoadOrderOrCreateNew();
        }

        #region Загрузка справочников

        private void LoadDictionaries()
        {
            using (var db = new AtlasServiceCenterEntities())
            {
                // Клиенты
                var clients = db.Clients
                                .OrderBy(c => c.FullName)
                                .ToList();
                ClientComboBox.ItemsSource = clients;

                // Статусы
                var statuses = db.OrderStatuses
                                 .OrderBy(s => s.OrderIndex)
                                 .ToList();
                StatusComboBox.ItemsSource = statuses;

                // Мастера (по должности "Мастер")
                var masters = (from e in db.Employees
                               join p in db.Positions on e.PositionId equals p.PositionId
                               where p.Name == "Мастер" && e.IsActive == true
                               orderby e.FullName
                               select e).ToList();
                MasterComboBox.ItemsSource = masters;
            }
        }

        #endregion

        #region Загрузка / создание заказа

        private void LoadOrderOrCreateNew()
        {
            using (var db = new AtlasServiceCenterEntities())
            {
                if (_orderId.HasValue)
                {
                    var order = db.RepairOrders.FirstOrDefault(o => o.OrderId == _orderId.Value);
                    if (order == null)
                    {
                        MessageBox.Show("Заказ не найден.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    TitleText.Text = $"Заказ №{order.OrderId}";

                    ClientComboBox.SelectedValue = order.ClientId;
                    LoadDevicesForClient(order.ClientId);
                    DeviceComboBox.SelectedValue = order.DeviceId;

                    StatusComboBox.SelectedValue = order.StatusId;
                    MasterComboBox.SelectedValue = order.MasterId;

                    DiscountTextBox.Text = order.DiscountPct.ToString("0.##", CultureInfo.InvariantCulture);
                    TotalAmountTextBox.Text = order.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture);

                    IsPaidCheckBox.IsChecked = order.IsPaid;
                    IsIssuedCheckBox.IsChecked = order.IsIssued;

                    ProblemTextBox.Text = order.ProblemDesc;
                    CommentTextBox.Text = order.Comment;
                }
                else
                {
                    TitleText.Text = "Новый заказ";

                    // Статус по умолчанию
                    var firstStatusId = db.OrderStatuses
                                          .OrderBy(s => s.OrderIndex)
                                          .Select(s => (int?)s.StatusId)
                                          .FirstOrDefault();
                    if (firstStatusId.HasValue)
                        StatusComboBox.SelectedValue = firstStatusId.Value;

                    // Мастер = текущий сотрудник, если есть
                    if (_currentUser.EmployeeId.HasValue)
                        MasterComboBox.SelectedValue = _currentUser.EmployeeId.Value;

                    DiscountTextBox.Text = "0";
                    TotalAmountTextBox.Text = "0.00";
                }
            }

            LoadWorksAndParts();
            UpdateAccessByRole();
        }

        #endregion

        #region Устройства клиента

        private void ClientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClientComboBox.SelectedValue is int clientId)
            {
                LoadDevicesForClient(clientId);
            }
        }

        private void LoadDevicesForClient(int clientId)
        {
            using (var db = new AtlasServiceCenterEntities())
            {
                var devices = db.Devices
                                .Where(d => d.ClientId == clientId)
                                .Select(d => new
                                {
                                    d.DeviceId,
                                    DeviceDisplay = d.DeviceType + " " +
                                                    (d.Brand ?? "") + " " +
                                                    (d.Model ?? "")
                                })
                                .ToList();

                DeviceComboBox.ItemsSource = devices;
            }
        }

        #endregion

        #region Работы и запчасти: загрузка и пересчёт

        private void LoadWorksAndParts()
        {
            if (!_orderId.HasValue)
            {
                WorksGrid.ItemsSource = null;
                PartsGrid.ItemsSource = null;
                return;
            }

            using (var db = new AtlasServiceCenterEntities())
            {
                var works = db.RepairOrderWorks
                              .Where(w => w.OrderId == _orderId.Value)
                              .OrderBy(w => w.WorkId)
                              .ToList();
                WorksGrid.ItemsSource = works;

                var parts = (from op in db.RepairOrderParts
                             join p in db.Parts on op.PartId equals p.PartId
                             where op.OrderId == _orderId.Value
                             orderby op.OrderPartId
                             select new OrderPartRow
                             {
                                 OrderPartId = op.OrderPartId,
                                 PartId = p.PartId,
                                 PartName = p.Name,
                                 PricePerUnit = op.PricePerUnit,
                                 Quantity = op.Quantity,
                                 LineTotal = op.LineTotal
                             }).ToList();
                PartsGrid.ItemsSource = parts;
            }

            RecalculateTotalsFromDb();
        }

        private void RecalculateButton_Click(object sender, RoutedEventArgs e)
        {
            RecalculateTotalsFromDb();
        }

        private void RecalculateTotalsFromDb()
        {
            decimal worksSum = 0;
            decimal partsSum = 0;

            if (_orderId.HasValue)
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    worksSum = db.RepairOrderWorks
                                 .Where(w => w.OrderId == _orderId.Value)
                                 .Select(w => (decimal?)w.LineTotal)
                                 .Sum() ?? 0;

                    partsSum = db.RepairOrderParts
                                 .Where(p => p.OrderId == _orderId.Value)
                                 .Select(p => (decimal?)p.LineTotal)
                                 .Sum() ?? 0;
                }
            }

            var discountPct = ParseDecimal(DiscountTextBox.Text);
            if (discountPct < 0) discountPct = 0;
            if (discountPct > 100) discountPct = 100;

            var totalBeforeDiscount = worksSum + partsSum;
            var totalAfterDiscount = totalBeforeDiscount * (1 - discountPct / 100m);

            TotalAmountTextBox.Text = totalAfterDiscount.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private void DiscountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecalculateTotalsFromDb();
        }

        #endregion

        #region Ролевая логика

        private void UpdateAccessByRole()
        {
            var roleName = _currentUser.Roles?.Name;

            bool canEditCore = RoleHelper.CanEditCoreOrderFields(roleName);
            bool canEditStatus = RoleHelper.CanEditStatus(roleName);
            bool canAssignMaster = RoleHelper.CanAssignMaster(roleName);
            bool canEditDiscount = RoleHelper.CanEditDiscount(roleName);
            bool canEditPrices = RoleHelper.CanEditPrices(roleName);

            ClientComboBox.IsEnabled = canEditCore;
            DeviceComboBox.IsEnabled = canEditCore;
            StatusComboBox.IsEnabled = canEditStatus;
            MasterComboBox.IsEnabled = canAssignMaster;

            DiscountTextBox.IsReadOnly = !canEditDiscount;
            IsPaidCheckBox.IsEnabled = RoleHelper.IsOwner(roleName) || RoleHelper.IsCashier(roleName);
            IsIssuedCheckBox.IsEnabled = RoleHelper.IsOwner(roleName) || RoleHelper.IsCashier(roleName);

            ProblemTextBox.IsReadOnly = !canEditCore && !RoleHelper.IsMaster(roleName);
            CommentTextBox.IsReadOnly = !canEditCore && !RoleHelper.IsMaster(roleName);

            if (RoleHelper.IsMaster(roleName))
            {
                EnableWorks();
                EnableParts();
            }
            else if (canEditCore)
            {
                EnableWorks();
                EnableParts();
            }
            else
            {
                DisableWorksAndParts();
            }

            _canEditPrices = canEditPrices;
        }

        private void DisableWorksAndParts()
        {
            WorksGrid.IsEnabled = false;
            PartsGrid.IsEnabled = false;
        }

        private void EnableWorks()
        {
            WorksGrid.IsEnabled = true;
        }

        private void EnableParts()
        {
            PartsGrid.IsEnabled = true;
        }

        #endregion

        #region Кнопки работ

        private bool EnsureOrderSaved()
        {
            if (_orderId.HasValue)
                return true;

            MessageBox.Show("Сначала сохраните заказ (кнопка \"Сохранить\"), затем добавляйте работы и запчасти.",
                "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        private RepairOrderWorks GetSelectedWork()
        {
            var row = WorksGrid.SelectedItem as RepairOrderWorks;
            return row;
        }

        private void AddWorkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureOrderSaved())
                return;

            var w = new WorkEditWindow(null, _canEditPrices);
            w.Owner = this;
            if (w.ShowDialog() != true)
                return;

            using (var db = new AtlasServiceCenterEntities())
            {
                var entity = new RepairOrderWorks
                {
                    OrderId = _orderId.Value,
                    WorkName = w.WorkName,
                    Price = w.Price,
                    Quantity = w.Quantity,
                    LineTotal = w.Price * w.Quantity
                };
                db.RepairOrderWorks.Add(entity);
                db.SaveChanges();
            }

            LoadWorksAndParts();
        }

        private void EditWorkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureOrderSaved())
                return;

            var selected = GetSelectedWork();
            if (selected == null)
            {
                MessageBox.Show("Выберите работу в списке.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var w = new WorkEditWindow(selected, _canEditPrices);
            w.Owner = this;
            if (w.ShowDialog() != true)
                return;

            using (var db = new AtlasServiceCenterEntities())
            {
                var entity = db.RepairOrderWorks.FirstOrDefault(r => r.WorkId == selected.WorkId);
                if (entity == null) return;

                entity.WorkName = w.WorkName;
                entity.Price = w.Price;
                entity.Quantity = w.Quantity;
                entity.LineTotal = w.Price * w.Quantity;

                db.SaveChanges();
            }

            LoadWorksAndParts();
        }

        private void DeleteWorkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureOrderSaved())
                return;

            var selected = GetSelectedWork();
            if (selected == null)
            {
                MessageBox.Show("Выберите работу в списке.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Удалить выбранную работу?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            using (var db = new AtlasServiceCenterEntities())
            {
                var entity = db.RepairOrderWorks.FirstOrDefault(r => r.WorkId == selected.WorkId);
                if (entity != null)
                {
                    db.RepairOrderWorks.Remove(entity);
                    db.SaveChanges();
                }
            }

            LoadWorksAndParts();
        }

        #endregion

        #region Кнопки запчастей

        private OrderPartRow GetSelectedPartRow()
        {
            return PartsGrid.SelectedItem as OrderPartRow;
        }

        private void AddPartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureOrderSaved())
                return;

            using (var db = new AtlasServiceCenterEntities())
            {
                var parts = db.Parts
                              .Where(p => p.IsActive && p.QuantityInStock > 0)
                              .OrderBy(p => p.Name)
                              .ToList();

                if (!parts.Any())
                {
                    MessageBox.Show("На складе нет активных запчастей.", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dlg = new PartEditWindow(parts, null, _canEditPrices);
                dlg.Owner = this;
                if (dlg.ShowDialog() != true)
                    return;

                var part = db.Parts.FirstOrDefault(p => p.PartId == dlg.SelectedPartId);
                if (part == null)
                    return;

                if (dlg.Quantity > part.QuantityInStock)
                {
                    MessageBox.Show("Недостаточно остатков на складе.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var orderPart = new RepairOrderParts
                {
                    OrderId = _orderId.Value,
                    PartId = part.PartId,
                    Quantity = dlg.Quantity,
                    PricePerUnit = dlg.PricePerUnit,
                    LineTotal = dlg.PricePerUnit * dlg.Quantity
                };
                db.RepairOrderParts.Add(orderPart);

                part.QuantityInStock -= dlg.Quantity;

                db.SaveChanges();
            }

            LoadWorksAndParts();
        }

        private void EditPartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureOrderSaved())
                return;

            var row = GetSelectedPartRow();
            if (row == null)
            {
                MessageBox.Show("Выберите запчасть в списке.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            using (var db = new AtlasServiceCenterEntities())
            {
                var orderPart = db.RepairOrderParts.FirstOrDefault(op => op.OrderPartId == row.OrderPartId);
                if (orderPart == null) return;

                var part = db.Parts.FirstOrDefault(p => p.PartId == orderPart.PartId);
                if (part == null) return;

                // возвращаем старое количество на склад перед редактированием
                part.QuantityInStock += orderPart.Quantity;

                db.SaveChanges();

                var dlg = new PartEditWindow(new[] { part }.ToList(), orderPart, _canEditPrices);
                dlg.Owner = this;
                if (dlg.ShowDialog() != true)
                {
                    // если отменили — вернуть обратно списанное количество
                    part.QuantityInStock -= orderPart.Quantity;
                    db.SaveChanges();
                    LoadWorksAndParts();
                    return;
                }

                if (dlg.Quantity > part.QuantityInStock)
                {
                    MessageBox.Show("Недостаточно остатков на складе.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    // возвращаем исходное списание
                    part.QuantityInStock -= orderPart.Quantity;
                    db.SaveChanges();
                    LoadWorksAndParts();
                    return;
                }

                // применяем новые значения
                orderPart.Quantity = dlg.Quantity;
                orderPart.PricePerUnit = dlg.PricePerUnit;
                orderPart.LineTotal = dlg.PricePerUnit * dlg.Quantity;

                part.QuantityInStock -= dlg.Quantity;

                db.SaveChanges();
            }

            LoadWorksAndParts();
        }

        private void DeletePartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureOrderSaved())
                return;

            var row = GetSelectedPartRow();
            if (row == null)
            {
                MessageBox.Show("Выберите запчасть в списке.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Удалить выбранную запчасть и вернуть её на склад?",
                    "Подтверждение", MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            using (var db = new AtlasServiceCenterEntities())
            {
                var orderPart = db.RepairOrderParts.FirstOrDefault(op => op.OrderPartId == row.OrderPartId);
                if (orderPart == null) return;

                var part = db.Parts.FirstOrDefault(p => p.PartId == orderPart.PartId);
                if (part != null)
                {
                    part.QuantityInStock += orderPart.Quantity;
                }

                db.RepairOrderParts.Remove(orderPart);
                db.SaveChanges();
            }

            LoadWorksAndParts();
        }

        #endregion

        #region Сохранение заказа / общие кнопки

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    RepairOrders entity;

                    if (_orderId.HasValue)
                    {
                        entity = db.RepairOrders.FirstOrDefault(o => o.OrderId == _orderId.Value);
                        if (entity == null)
                        {
                            MessageBox.Show("Заказ не найден при сохранении.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        entity = new RepairOrders
                        {
                            CreatedAt = DateTime.Now
                        };
                        db.RepairOrders.Add(entity);
                    }

                    if (ClientComboBox.SelectedValue is int clientId)
                        entity.ClientId = clientId;
                    else
                    {
                        MessageBox.Show("Выберите клиента.", "Внимание",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (DeviceComboBox.SelectedValue is int deviceId)
                        entity.DeviceId = deviceId;
                    else
                    {
                        MessageBox.Show("Выберите устройство клиента.", "Внимание",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (StatusComboBox.SelectedValue is int statusId)
                        entity.StatusId = statusId;

                    if (MasterComboBox.SelectedValue is int masterId)
                        entity.MasterId = masterId;

                    entity.DiscountPct = ParseDecimal(DiscountTextBox.Text);
                    entity.TotalAmount = ParseDecimal(TotalAmountTextBox.Text);

                    entity.IsPaid = IsPaidCheckBox.IsChecked == true;
                    entity.IsIssued = IsIssuedCheckBox.IsChecked == true;

                    entity.ProblemDesc = ProblemTextBox.Text;
                    entity.Comment = CommentTextBox.Text;

                    db.SaveChanges();

                    if (!_orderId.HasValue)
                    {
                        _orderId = entity.OrderId;
                        TitleText.Text = $"Заказ №{entity.OrderId}";
                    }
                }

                MessageBox.Show("Заказ сохранён.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении заказа:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Печать акта выполненных работ

        private void PrintActButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_orderId.HasValue)
            {
                MessageBox.Show("Сначала сохраните заказ перед печатью акта.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    int orderId = _orderId.Value;

                    var order = db.RepairOrders.FirstOrDefault(o => o.OrderId == orderId);
                    if (order == null)
                    {
                        MessageBox.Show("Заказ не найден в базе.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var client = db.Clients.FirstOrDefault(c => c.ClientId == order.ClientId);
                    var device = db.Devices.FirstOrDefault(d => d.DeviceId == order.DeviceId);
                    Employees master = null;
                    if (order.MasterId.HasValue)
                        master = db.Employees.FirstOrDefault(emp => emp.EmployeeId == order.MasterId.Value);

                    var works = db.RepairOrderWorks
                        .Where(w => w.OrderId == orderId)
                        .OrderBy(w => w.WorkId)
                        .ToList();

                    var parts = (from op in db.RepairOrderParts
                                 join p in db.Parts on op.PartId equals p.PartId
                                 where op.OrderId == orderId
                                 orderby op.OrderPartId
                                 select new
                                 {
                                     PartName = p.Name,
                                     op.Quantity,
                                     op.PricePerUnit,
                                     op.LineTotal
                                 }).ToList();

                    decimal worksSum = works.Select(w => (decimal?)w.LineTotal).Sum() ?? 0m;
                    decimal partsSum = parts.Select(p => (decimal?)p.LineTotal).Sum() ?? 0m;
                    decimal totalBeforeDiscount = worksSum + partsSum;
                    decimal discountPct = order.DiscountPct;
                    decimal totalAfterDiscount = order.TotalAmount;

                    FlowDocument doc = new FlowDocument
                    {
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 12,
                        PagePadding = new Thickness(40),
                        ColumnWidth = double.PositiveInfinity
                    };

                    var title = new Paragraph
                    {
                        TextAlignment = TextAlignment.Center,
                        FontSize = 16,
                        FontWeight = FontWeights.Bold
                    };
                    title.Inlines.Add(new Run($"АКТ ВЫПОЛНЕННЫХ РАБОТ № {orderId}"));
                    doc.Blocks.Add(title);

                    doc.Blocks.Add(new Paragraph(new Run($"от {DateTime.Now:dd.MM.yyyy}"))
                    {
                        TextAlignment = TextAlignment.Center
                    });

                    doc.Blocks.Add(new Paragraph(new Run(" ")));

                    doc.Blocks.Add(new Paragraph(new Run($"Клиент: {client?.FullName ?? ""}")));
                    doc.Blocks.Add(new Paragraph(new Run($"Телефон: {client?.Phone ?? ""}")));
                    doc.Blocks.Add(new Paragraph(new Run(
                        $"Устройство: {device?.DeviceType} {device?.Brand} {device?.Model}")));
                    doc.Blocks.Add(new Paragraph(new Run($"Серийный номер: {device?.SerialNumber ?? ""}")));
                    doc.Blocks.Add(new Paragraph(new Run($"Мастер: {master?.FullName ?? ""}")));
                    doc.Blocks.Add(new Paragraph(new Run($"Описание неисправности: {order.ProblemDesc ?? ""}")));

                    doc.Blocks.Add(new Paragraph(new Run(" ")));

                    if (works.Count > 0)
                    {
                        doc.Blocks.Add(new Paragraph(new Bold(new Run("Выполненные работы:"))));

                        int n = 1;
                        foreach (var w in works)
                        {
                            string line =
                                $"{n}. {w.WorkName}, кол-во: {w.Quantity}, цена: {w.Price:0.00} руб., сумма: {w.LineTotal:0.00} руб.";
                            doc.Blocks.Add(new Paragraph(new Run(line)));
                            n++;
                        }

                        doc.Blocks.Add(new Paragraph(new Run(" ")));
                    }

                    if (parts.Count > 0)
                    {
                        doc.Blocks.Add(new Paragraph(new Bold(new Run("Использованные запчасти:"))));

                        int n = 1;
                        foreach (var p in parts)
                        {
                            string line =
                                $"{n}. {p.PartName}, кол-во: {p.Quantity}, цена: {p.PricePerUnit:0.00} руб., сумма: {p.LineTotal:0.00} руб.";
                            doc.Blocks.Add(new Paragraph(new Run(line)));
                            n++;
                        }

                        doc.Blocks.Add(new Paragraph(new Run(" ")));
                    }

                    doc.Blocks.Add(new Paragraph(new Run($"Стоимость работ: {worksSum:0.00} руб.")));
                    doc.Blocks.Add(new Paragraph(new Run($"Стоимость запчастей: {partsSum:0.00} руб.")));
                    doc.Blocks.Add(new Paragraph(new Run($"Итого до скидки: {totalBeforeDiscount:0.00} руб.")));
                    doc.Blocks.Add(new Paragraph(new Run($"Скидка: {discountPct:0.##}%")));
                    doc.Blocks.Add(new Paragraph(new Bold(
                        new Run($"Итого к оплате: {totalAfterDiscount:0.00} руб."))));

                    doc.Blocks.Add(new Paragraph(new Run(" ")));

                    doc.Blocks.Add(new Paragraph(new Run(
                        "Мастер _______________________  / " + (master?.FullName ?? "") + " /")));
                    doc.Blocks.Add(new Paragraph(new Run(
                        "Клиент _______________________  / " + (client?.FullName ?? "") + " /")));

                    PrintDialog dlg = new PrintDialog();
                    if (dlg.ShowDialog() == true)
                    {
                        dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator,
                            "Акт выполненных работ");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка печати акта:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Вспомогательные

        private decimal ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            text = text.Replace(',', '.');

            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                return value;

            return 0;
        }

        public class OrderPartRow
        {
            public int OrderPartId { get; set; }
            public int PartId { get; set; }
            public string PartName { get; set; }
            public decimal PricePerUnit { get; set; }
            public int Quantity { get; set; }
            public decimal LineTotal { get; set; }
        }

        #endregion
    }
}
