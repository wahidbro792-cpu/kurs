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
using AtlasServiceCenter.Windows;
using UserEntity = AtlasServiceCenter.Users;  // сущность Users из EF

namespace AtlasServiceCenter.Windows
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = string.Empty;

            var login = LoginTextBox.Text?.Trim();
            var password = PasswordBox.Password?.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ErrorTextBlock.Text = "Введите логин и пароль.";
                return;
            }

            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    // Подтягиваем роль
                    var user = db.Users
                                 .Include("Roles")
                                 .FirstOrDefault(u => u.Login == login && u.Password == password);

                    if (user == null)
                    {
                        ErrorTextBlock.Text = "Неверный логин или пароль.";
                        return;
                    }

                    if (user.EmployeeId.HasValue)
                    {
                        var employee = db.Employees.FirstOrDefault(emp => emp.EmployeeId == user.EmployeeId.Value);
                        if (employee != null && !employee.IsActive)
                        {
                            ErrorTextBlock.Text = "Доступ отключен: сотрудник уволен.";
                            return;
                        }
                    }

                    // Успешный вход — открываем главное окно
                    var mainWindow = new MainWindow(user);
                    mainWindow.Show();

                    // Закрываем окно логина
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при работе с базой данных:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Если пользователь просто закрыл окно логина — гасим приложение
            if (Application.Current.MainWindow == this)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
