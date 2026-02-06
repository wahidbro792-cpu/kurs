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
using Microsoft.Win32;
using System.IO;


namespace AtlasServiceCenter.Windows
{
    public partial class UserWindow : Window
    {
        private readonly int? _userId;
        private string _selectedPhotoTempPath; // временный путь выбранного файла

        public UserWindow(int? userId)
        {
            InitializeComponent();
            _userId = userId;

            LoadRoles();
            LoadEmployees();
            LoadUser();
        }

        private void LoadRoles()
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var roles = db.Roles
                                  .OrderBy(r => r.Name)
                                  .ToList();
                    RoleComboBox.ItemsSource = roles;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки ролей:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEmployees()
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var employees = db.Employees
                                      .Where(emp => emp.IsActive == true)
                                      .OrderBy(emp => emp.FullName)
                                      .ToList();
                    EmployeeComboBox.ItemsSource = employees;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки сотрудников:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUser()
        {
            if (!_userId.HasValue)
            {
                Title = "Новый пользователь";
                // Для нового пользователя фото нет, просто ничего не грузим
                return;
            }

            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var user = db.Users.FirstOrDefault(u => u.UserId == _userId.Value);
                    if (user == null)
                    {
                        MessageBox.Show("Пользователь не найден.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    Title = "Пользователь: " + user.Login;

                    LoginBox.Text = user.Login;
                    // Пароль по-хорошему не показываем, оставляем пустым — можно задать новый
                    PasswordBox.Password = string.Empty;
                    RoleComboBox.SelectedValue = user.RoleId;
                    EmployeeComboBox.SelectedValue = user.EmployeeId;

                    // Загружаем фото, если есть
                    LoadUserPhotoIfExists(user.UserId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пользователя:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserPhotoIfExists(int userId)
        {
            try
            {
                string path = UserPhotoHelper.GetUserPhotoPath(userId);
                if (!File.Exists(path))
                {
                    UserPhotoImage.Source = null;
                    return;
                }

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.EndInit();
                UserPhotoImage.Source = bmp;
            }
            catch
            {
                UserPhotoImage.Source = null;
            }
        }

        private void SelectPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp",
                Title = "Выбор фотографии пользователя"
            };

            if (dlg.ShowDialog() == true)
            {
                _selectedPhotoTempPath = dlg.FileName;

                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bmp.UriSource = new Uri(_selectedPhotoTempPath, UriKind.Absolute);
                    bmp.EndInit();
                    UserPhotoImage.Source = bmp;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось загрузить выбранное изображение:\n" + ex.Message,
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    _selectedPhotoTempPath = null;
                    UserPhotoImage.Source = null;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string login = (LoginBox.Text ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(login))
                {
                    MessageBox.Show("Введите логин.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (RoleComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Выберите роль.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string password = PasswordBox.Password;
                bool isNewUser = !_userId.HasValue;

                if (isNewUser)
                {
                    if (string.IsNullOrEmpty(password))
                    {
                        MessageBox.Show("Для нового пользователя нужно задать пароль.", "Внимание",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                int savedUserId;

                using (var db = new AtlasServiceCenterEntities())
                {
                    // Проверка на уникальность логина
                    var existingWithSameLogin = db.Users
                        .FirstOrDefault(u => u.Login == login && u.UserId != (_userId.HasValue ? _userId.Value : 0));
                    if (existingWithSameLogin != null)
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует.", "Внимание",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    Users user;

                    if (_userId.HasValue)
                    {
                        user = db.Users.FirstOrDefault(u => u.UserId == _userId.Value);
                        if (user == null)
                        {
                            MessageBox.Show("Пользователь не найден при сохранении.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        user = new Users();
                        db.Users.Add(user);
                    }

                    user.Login = login;
                    user.RoleId = (int)RoleComboBox.SelectedValue;

                    if (EmployeeComboBox.SelectedValue != null)
                        user.EmployeeId = (int?)EmployeeComboBox.SelectedValue;
                    else
                        user.EmployeeId = null;

                    // Пароль: для простоты курсового — в открытом виде.
                    if (!string.IsNullOrEmpty(password))
                    {
                        user.Password = password;
                    }

                    db.SaveChanges();
                    savedUserId = user.UserId;
                }

                // Сохраняем фото (если выбрано)
                if (!string.IsNullOrEmpty(_selectedPhotoTempPath) &&
                    File.Exists(_selectedPhotoTempPath) &&
                    savedUserId > 0)
                {
                    string target = UserPhotoHelper.GetUserPhotoPath(savedUserId);
                    try
                    {
                        File.Copy(_selectedPhotoTempPath, target, overwrite: true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Не удалось сохранить фото пользователя:\n" + ex.Message,
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // Обновляем аватар в шапке, если главное окно открыто
                var main = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                if (main != null)
                {
                    main.ReloadCurrentUserHeaderInfo();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения пользователя:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
