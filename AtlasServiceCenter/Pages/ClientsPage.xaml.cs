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
    public partial class ClientsPage : UserControl
    {
        private ObservableCollection<Clients> _allClients;

        public ClientsPage()
        {
            InitializeComponent();
            LoadClients();
        }

        private void LoadClients()
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var list = db.Clients
                                 .OrderBy(c => c.FullName)
                                 .ToList();

                    _allClients = new ObservableCollection<Clients>(list);
                    ClientsGrid.ItemsSource = _allClients;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки клиентов:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            LoadClients();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_allClients == null)
                return;

            string text = SearchBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(text))
            {
                ClientsGrid.ItemsSource = _allClients;
                return;
            }

            var filtered = _allClients
                .Where(c =>
                    (!string.IsNullOrEmpty(c.FullName) &&
                     c.FullName.ToLower().Contains(text)) ||
                    (!string.IsNullOrEmpty(c.Phone) &&
                     c.Phone.ToLower().Contains(text)) ||
                    (!string.IsNullOrEmpty(c.Email) &&
                     c.Email.ToLower().Contains(text)))
                .ToList();

            ClientsGrid.ItemsSource = filtered;
        }

        private Clients GetSelectedClient()
        {
            return ClientsGrid.SelectedItem as Clients;
        }

        private void ClientsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var client = GetSelectedClient();
            if (client != null)
            {
                OpenClient(client);
            }
        }

        private void OpenClient(Clients client)
        {
            if (client == null)
            {
                MessageBox.Show("Выберите клиента.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var w = new ClientWindow(client.ClientId);
            w.Owner = Window.GetWindow(this);
            w.ShowDialog();

            LoadClients();
        }

        private void NewClientButton_Click(object sender, RoutedEventArgs e)
        {
            var w = new ClientWindow(null);
            w.Owner = Window.GetWindow(this);
            w.ShowDialog();

            LoadClients();
        }

        // -------------------------
        //      УДАЛЕНИЕ КЛИЕНТА
        // -------------------------
        private void DeleteClientButton_Click(object sender, RoutedEventArgs e)
        {
            var client = GetSelectedClient();
            if (client == null)
            {
                MessageBox.Show("Выберите клиента для удаления.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                    $"Удалить клиента:\n{client.FullName}?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var c = db.Clients.FirstOrDefault(x => x.ClientId == client.ClientId);
                    if (c == null)
                    {
                        MessageBox.Show("Клиент не найден.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Все заказы клиента
                    var clientOrders = db.RepairOrders
                        .Where(o => o.ClientId == c.ClientId)
                        .ToList();

                    // Есть незавершённые (невыданные) заказы?
                    bool hasNotIssued = clientOrders.Any(o => !o.IsIssued);

                    if (hasNotIssued)
                    {
                        MessageBox.Show(
                            "Нельзя удалить клиента — у него есть незавершённые (невыданные) заказы.\n" +
                            "Сначала закройте и выдайте все заказы или удалите их.",
                            "Удаление запрещено",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // Если заказы есть, но все выданы — удаляем их
                    if (clientOrders.Count > 0)
                    {
                        // У RepairOrders есть каскад на RepairOrderWorks/Parts,
                        // поэтому сначала удаляем заказы, потом клиента.
                        db.RepairOrders.RemoveRange(clientOrders);
                    }

                    // У Devices стоит ON DELETE CASCADE по ClientId,
                    // поэтому при удалении клиента его устройства удалятся автоматически.
                    db.Clients.Remove(c);

                    db.SaveChanges();
                }

                LoadClients();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления клиента:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
