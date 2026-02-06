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

namespace AtlasServiceCenter.Pages
{
    public partial class SettingsPage : UserControl
    {
        private ObservableCollection<OrderStatuses> _statuses;

        // справочники на основе таблицы Devices
        private ObservableCollection<DeviceTypeRow> _deviceTypes;
        private ObservableCollection<BrandRow> _brands;

        public SettingsPage()
        {
            InitializeComponent();
            LoadStatuses();
            LoadDeviceLookups();
        }

        #region Статусы заказов

        private void LoadStatuses()
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var list = db.OrderStatuses
                                 .OrderBy(s => s.OrderIndex)
                                 .ToList();

                    _statuses = new ObservableCollection<OrderStatuses>(list);
                    StatusesGrid.ItemsSource = _statuses;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки статусов:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (_statuses == null)
                _statuses = new ObservableCollection<OrderStatuses>();

            int nextIndex = 1;
            if (_statuses.Any())
                nextIndex = _statuses.Max(s => s.OrderIndex) + 1;

            var newStatus = new OrderStatuses
            {
                Name = "Новый статус",
                OrderIndex = nextIndex
            };

            _statuses.Add(newStatus);
            StatusesGrid.ItemsSource = _statuses;
            StatusesGrid.SelectedItem = newStatus;
            StatusesGrid.ScrollIntoView(newStatus);
        }

        #endregion

        #region Справочники устройств и брендов

        private void LoadDeviceLookups()
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    // Типы устройств
                    var typesData = db.Devices
                        .Where(d => d.DeviceType != null && d.DeviceType != "")
                        .GroupBy(d => d.DeviceType)
                        .Select(g => new
                        {
                            Name = g.Key,
                            Count = g.Count()
                        })
                        .OrderBy(x => x.Name)
                        .ToList();

                    _deviceTypes = new ObservableCollection<DeviceTypeRow>(
                        typesData.Select(t => new DeviceTypeRow
                        {
                            Name = t.Name,
                            NewName = t.Name,
                            DevicesCount = t.Count
                        }));

                    DeviceTypesGrid.ItemsSource = _deviceTypes;

                    // Бренды
                    var brandsData = db.Devices
                        .Where(d => d.Brand != null && d.Brand != "")
                        .GroupBy(d => d.Brand)
                        .Select(g => new
                        {
                            Name = g.Key,
                            Count = g.Count()
                        })
                        .OrderBy(x => x.Name)
                        .ToList();

                    _brands = new ObservableCollection<BrandRow>(
                        brandsData.Select(b => new BrandRow
                        {
                            Name = b.Name,
                            NewName = b.Name,
                            DevicesCount = b.Count
                        }));

                    BrandsGrid.ItemsSource = _brands;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки справочников устройств/брендов:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Массовое переименование типов устройств и брендов в таблице Devices.
        /// Вызывается из SaveButton_Click в том же DbContext.
        /// </summary>
        private void SaveDeviceLookups(AtlasServiceCenterEntities db)
        {
            // Типы устройств
            if (_deviceTypes != null)
            {
                foreach (var row in _deviceTypes)
                {
                    if (string.IsNullOrWhiteSpace(row.Name))
                        continue;

                    var newName = (row.NewName ?? string.Empty).Trim();

                    // если имя не изменилось или новое пустое – пропускаем
                    if (string.IsNullOrEmpty(newName) || newName == row.Name)
                        continue;

                    var devices = db.Devices
                        .Where(d => d.DeviceType == row.Name)
                        .ToList();

                    foreach (var dev in devices)
                    {
                        dev.DeviceType = newName;
                    }
                }
            }

            // Бренды
            if (_brands != null)
            {
                foreach (var row in _brands)
                {
                    if (string.IsNullOrWhiteSpace(row.Name))
                        continue;

                    var newName = (row.NewName ?? string.Empty).Trim();

                    if (string.IsNullOrEmpty(newName) || newName == row.Name)
                        continue;

                    var devices = db.Devices
                        .Where(d => d.Brand == row.Name)
                        .ToList();

                    foreach (var dev in devices)
                    {
                        dev.Brand = newName;
                    }
                }
            }
        }

        #endregion

        #region Сохранение и обновление

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    // Сохранение статусов
                    if (_statuses != null)
                    {
                        foreach (var status in _statuses)
                        {
                            if (string.IsNullOrWhiteSpace(status.Name))
                            {
                                MessageBox.Show("Название статуса не может быть пустым.",
                                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            if (status.StatusId == 0)
                            {
                                var newEntity = new OrderStatuses
                                {
                                    Name = status.Name.Trim(),
                                    OrderIndex = status.OrderIndex
                                };
                                db.OrderStatuses.Add(newEntity);
                            }
                            else
                            {
                                var existing = db.OrderStatuses
                                                 .FirstOrDefault(s => s.StatusId == status.StatusId);
                                if (existing != null)
                                {
                                    existing.Name = status.Name.Trim();
                                    existing.OrderIndex = status.OrderIndex;
                                }
                            }
                        }
                    }

                    // Сохранение справочников устройств/брендов
                    SaveDeviceLookups(db);

                    db.SaveChanges();
                }

                MessageBox.Show("Настройки сохранены.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadStatuses();
                LoadDeviceLookups();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения настроек:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadStatuses();
            LoadDeviceLookups();
        }

        #endregion

        #region Внутренние модели строк

        public class DeviceTypeRow
        {
            public string Name { get; set; }          // текущее имя в БД
            public string NewName { get; set; }       // новое имя (для переименования)
            public int DevicesCount { get; set; }     // сколько устройств с таким типом
        }

        public class BrandRow
        {
            public string Name { get; set; }
            public string NewName { get; set; }
            public int DevicesCount { get; set; }
        }

        #endregion
    }
}
