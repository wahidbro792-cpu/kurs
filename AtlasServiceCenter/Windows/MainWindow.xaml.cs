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
using AtlasServiceCenter.Pages;
using UserEntity = AtlasServiceCenter.Users;


namespace AtlasServiceCenter.Windows
{
    public partial class MainWindow : Window
    {
        private readonly UserEntity _currentUser;

        // Конструктор для дизайнера
        public MainWindow()
        {
            InitializeComponent();
        }

        // Основной конструктор – сюда приходит пользователь из LoginWindow
        public MainWindow(UserEntity currentUser) : this()
        {
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            ReloadCurrentUserHeaderInfo();
            ConfigureMenuByRole(_currentUser.Roles?.Name);

            // Стартовая страница
            OpenDashboard();
        }

        // Публичный метод, чтобы окна могли обновить шапку
        public void ReloadCurrentUserHeaderInfo()
        {
            UserLoginText.Text = _currentUser.Login;
            UserRoleText.Text = _currentUser.Roles?.Name ?? "Без роли";

            // Загружаем фото пользователя, если есть
            try
            {
                if (_currentUser.UserId > 0 && UserPhotoHelper.UserPhotoExists(_currentUser.UserId))
                {
                    string path = UserPhotoHelper.GetUserPhotoPath(_currentUser.UserId);

                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bmp.UriSource = new Uri(path, UriKind.Absolute);
                    bmp.EndInit();
                    CurrentUserPhoto.Source = bmp;
                }
                else
                {
                    CurrentUserPhoto.Source = null;
                }
            }
            catch
            {
                CurrentUserPhoto.Source = null;
            }
        }

        private void ConfigureMenuByRole(string roleName)
        {
            // Сначала всё включаем
            BtnDashboard.Visibility = Visibility.Visible;
            BtnOrders.Visibility = Visibility.Visible;
            BtnCashier.Visibility = Visibility.Visible;
            BtnClients.Visibility = Visibility.Visible;
            BtnEmployees.Visibility = Visibility.Visible;
            BtnParts.Visibility = Visibility.Visible;
            BtnUsers.Visibility = Visibility.Visible;
            BtnReports.Visibility = Visibility.Visible;
            BtnSalary.Visibility = Visibility.Visible;
            BtnSettings.Visibility = Visibility.Visible;

            switch (roleName)
            {
                case "Владелец":
                    // Полный доступ
                    break;

                case "Администратор":
                    // Всё, кроме зарплаты
                    BtnSalary.Visibility = Visibility.Collapsed;
                    break;

                case "Менеджер":
                    BtnEmployees.Visibility = Visibility.Collapsed;
                    BtnParts.Visibility = Visibility.Collapsed;
                    BtnUsers.Visibility = Visibility.Collapsed;
                    BtnSalary.Visibility = Visibility.Collapsed;
                    BtnSettings.Visibility = Visibility.Collapsed;
                    // Кассу менеджеру не даём
                    BtnCashier.Visibility = Visibility.Collapsed;
                    break;

                case "Мастер":
                    BtnClients.Visibility = Visibility.Collapsed;
                    BtnEmployees.Visibility = Visibility.Collapsed;
                    BtnUsers.Visibility = Visibility.Collapsed;
                    BtnReports.Visibility = Visibility.Collapsed;
                    BtnSalary.Visibility = Visibility.Collapsed;
                    BtnSettings.Visibility = Visibility.Collapsed;
                    BtnCashier.Visibility = Visibility.Collapsed;
                    // Склад (BtnParts) можно оставить или скрыть по желанию
                    break;

                case "Кассир":
                    // Кассиру оставляем панель, заказы и кассу
                    BtnClients.Visibility = Visibility.Collapsed;
                    BtnEmployees.Visibility = Visibility.Collapsed;
                    BtnParts.Visibility = Visibility.Collapsed;
                    BtnUsers.Visibility = Visibility.Collapsed;
                    BtnReports.Visibility = Visibility.Collapsed;
                    BtnSalary.Visibility = Visibility.Collapsed;
                    BtnSettings.Visibility = Visibility.Collapsed;
                    break;

                default:
                    // Минимальный доступ
                    BtnClients.Visibility = Visibility.Collapsed;
                    BtnEmployees.Visibility = Visibility.Collapsed;
                    BtnParts.Visibility = Visibility.Collapsed;
                    BtnUsers.Visibility = Visibility.Collapsed;
                    BtnReports.Visibility = Visibility.Collapsed;
                    BtnSalary.Visibility = Visibility.Collapsed;
                    BtnSettings.Visibility = Visibility.Collapsed;
                    BtnCashier.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        #region Навигация

        private void OpenDashboard() => MainContent.Content = new DashboardPage();

        private void OpenOrders() => MainContent.Content = new OrdersPage(_currentUser);

        private void OpenCashier() => MainContent.Content = new CashierPage();

        private void OpenClients() => MainContent.Content = new ClientsPage();

        private void OpenEmployees() => MainContent.Content = new EmployeesPage();

        private void OpenParts() => MainContent.Content = new PartsPage();

        private void OpenUsers() => MainContent.Content = new UsersPage(_currentUser);

        private void OpenReports() => MainContent.Content = new ReportsPage();

        private void OpenSalary() => MainContent.Content = new SalaryPage();

        private void OpenSettings() => MainContent.Content = new SettingsPage();

        private void BtnDashboard_Click(object sender, RoutedEventArgs e) => OpenDashboard();

        private void BtnOrders_Click(object sender, RoutedEventArgs e) => OpenOrders();

        private void BtnCashier_Click(object sender, RoutedEventArgs e) => OpenCashier();

        private void BtnClients_Click(object sender, RoutedEventArgs e) => OpenClients();

        private void BtnEmployees_Click(object sender, RoutedEventArgs e) => OpenEmployees();

        private void BtnParts_Click(object sender, RoutedEventArgs e) => OpenParts();

        private void BtnUsers_Click(object sender, RoutedEventArgs e) => OpenUsers();

        private void BtnReports_Click(object sender, RoutedEventArgs e) => OpenReports();

        private void BtnSalary_Click(object sender, RoutedEventArgs e) => OpenSalary();

        private void BtnSettings_Click(object sender, RoutedEventArgs e) => OpenSettings();

        #endregion

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }
}
