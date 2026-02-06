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
    public partial class ClientWindow : Window
    {
        private readonly int? _clientId;

        public ClientWindow(int? clientId)
        {
            InitializeComponent();
            _clientId = clientId;

            LoadClient();
        }

        private void LoadClient()
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    if (_clientId.HasValue)
                    {
                        var client = db.Clients.FirstOrDefault(c => c.ClientId == _clientId.Value);
                        if (client == null)
                        {
                            MessageBox.Show("Клиент не найден.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            Close();
                            return;
                        }

                        Title = "Клиент: " + client.FullName;

                        FullNameBox.Text = client.FullName;
                        PhoneBox.Text = client.Phone;
                        EmailBox.Text = client.Email;
                        NotesBox.Text = client.Notes;

                        var devs = db.Devices
                                     .Where(d => d.ClientId == client.ClientId)
                                     .Select(d => new
                                     {
                                         d.DeviceId,
                                         d.DeviceType,
                                         d.Brand,
                                         d.Model,
                                         d.SerialNumber
                                     })
                                     .ToList();

                        DevicesGrid.ItemsSource = devs;
                    }
                    else
                    {
                        Title = "Новый клиент";
                        DevicesGrid.ItemsSource = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки клиента:\n" + ex.Message,
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
                    Clients entity;

                    if (_clientId.HasValue)
                    {
                        entity = db.Clients.FirstOrDefault(c => c.ClientId == _clientId.Value);
                        if (entity == null)
                        {
                            MessageBox.Show("Клиент не найден при сохранении.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        entity = new Clients();
                        db.Clients.Add(entity);
                    }

                    entity.FullName = FullNameBox.Text.Trim();
                    entity.Phone = PhoneBox.Text.Trim();
                    entity.Email = EmailBox.Text.Trim();
                    entity.Notes = NotesBox.Text.Trim();

                    if (string.IsNullOrWhiteSpace(entity.FullName))
                    {
                        MessageBox.Show("ФИО обязательно для заполнения.", "Внимание",
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
                    "Ошибка сохранения клиента:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AddDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_clientId.HasValue)
            {
                MessageBox.Show("Сначала сохраните клиента.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var w = new DeviceWindow(_clientId.Value, null);
            w.Owner = this;
            w.ShowDialog();

            LoadClient();
        }

        private void DevicesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_clientId.HasValue || DevicesGrid.SelectedItem == null)
                return;

            dynamic row = DevicesGrid.SelectedItem;
            int deviceId = row.DeviceId;

            var w = new DeviceWindow(_clientId.Value, deviceId);
            w.Owner = this;
            w.ShowDialog();

            LoadClient();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

