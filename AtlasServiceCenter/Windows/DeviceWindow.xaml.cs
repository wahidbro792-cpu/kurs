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

namespace AtlasServiceCenter.Windows
{
    public partial class DeviceWindow : Window
    {
        private readonly int _clientId;
        private readonly int? _deviceId;

        public DeviceWindow(int clientId, int? deviceId)
        {
            InitializeComponent();
            _clientId = clientId;
            _deviceId = deviceId;

            LoadLookups();
            LoadDevice();
        }

        private void LoadLookups()
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    // Уникальные типы устройств
                    var deviceTypes = db.Devices
                        .Where(d => d.DeviceType != null && d.DeviceType != "")
                        .Select(d => d.DeviceType)
                        .Distinct()
                        .OrderBy(s => s)
                        .ToList();

                    DeviceTypeBox.ItemsSource = deviceTypes;

                    // Уникальные бренды
                    var brands = db.Devices
                        .Where(d => d.Brand != null && d.Brand != "")
                        .Select(d => d.Brand)
                        .Distinct()
                        .OrderBy(s => s)
                        .ToList();

                    BrandBox.ItemsSource = brands;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки справочников устройств и брендов:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadDevice()
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    if (_deviceId.HasValue)
                    {
                        var device = db.Devices.FirstOrDefault(d => d.DeviceId == _deviceId.Value);
                        if (device == null)
                        {
                            MessageBox.Show("Устройство не найдено.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            Close();
                            return;
                        }

                        Title = "Устройство: " + device.DeviceType;

                        // Для ComboBox с IsEditable продолжаем использовать Text
                        DeviceTypeBox.Text = device.DeviceType;
                        BrandBox.Text = device.Brand;
                        ModelBox.Text = device.Model;
                        SerialBox.Text = device.SerialNumber;
                        NotesBox.Text = device.Notes;
                    }
                    else
                    {
                        Title = "Новое устройство";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки устройства:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    Devices entity;

                    if (_deviceId.HasValue)
                    {
                        entity = db.Devices.FirstOrDefault(d => d.DeviceId == _deviceId.Value);
                        if (entity == null)
                        {
                            MessageBox.Show("Устройство не найдено при сохранении.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        entity = new Devices
                        {
                            ClientId = _clientId
                        };
                        db.Devices.Add(entity);
                    }

                    // ComboBox.IsEditable → берём Text
                    entity.DeviceType = (DeviceTypeBox.Text ?? string.Empty).Trim();
                    entity.Brand = (BrandBox.Text ?? string.Empty).Trim();
                    entity.Model = (ModelBox.Text ?? string.Empty).Trim();
                    entity.SerialNumber = (SerialBox.Text ?? string.Empty).Trim();
                    entity.Notes = (NotesBox.Text ?? string.Empty).Trim();

                    if (string.IsNullOrWhiteSpace(entity.DeviceType))
                    {
                        MessageBox.Show("Тип устройства обязателен.", "Внимание",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    db.SaveChanges();
                }

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка сохранения устройства:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
