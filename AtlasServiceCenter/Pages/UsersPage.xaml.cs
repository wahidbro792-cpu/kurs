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
using UserEntity = AtlasServiceCenter.Users;
using System.IO;


namespace AtlasServiceCenter.Pages
{
    public partial class UsersPage : UserControl
    {
        private readonly UserEntity _currentUser;
        private ObservableCollection<UserRow> _allUsers;

        public UsersPage(UserEntity currentUser)
        {
            InitializeComponent();

            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            ConfigureButtonsByRole(_currentUser.Roles?.Name);
            LoadUsers();
        }

        private void ConfigureButtonsByRole(string roleName)
        {
            // Только Владелец и Администратор управляют пользователями
            bool canManage = RoleHelper.CanManageUsers(roleName);

            NewUserButton.IsEnabled = canManage;
            EditUserButton.IsEnabled = canManage;
            DeleteUserButton.IsEnabled = canManage;
        }

        private void LoadUsers()
        {
            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var list = (from u in db.Users
                                join r in db.Roles
                                    on u.RoleId equals r.RoleId
                                join e in db.Employees
                                    on u.EmployeeId equals (int?)e.EmployeeId into empJoin
                                from e in empJoin.DefaultIfEmpty()
                                orderby u.Login
                                select new UserRow
                                {
                                    UserId = u.UserId,
                                    Login = u.Login,
                                    RoleName = r.Name,
                                    EmployeeName = e != null ? e.FullName : null
                                })
                               .ToList();

                    // Подгружаем фото (BitmapImage с IgnoreImageCache)
                    foreach (var row in list)
                    {
                        if (UserPhotoHelper.UserPhotoExists(row.UserId))
                        {
                            string path = UserPhotoHelper.GetUserPhotoPath(row.UserId);
                            try
                            {
                                var bmp = new BitmapImage();
                                bmp.BeginInit();
                                bmp.CacheOption = BitmapCacheOption.OnLoad;
                                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                                bmp.UriSource = new Uri(path, UriKind.Absolute);
                                bmp.EndInit();
                                row.PhotoImage = bmp;
                            }
                            catch
                            {
                                row.PhotoImage = null;
                            }
                        }
                        else
                        {
                            row.PhotoImage = null;
                        }
                    }

                    _allUsers = new ObservableCollection<UserRow>(list);
                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пользователей:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            if (_allUsers == null)
                return;

            string search = (SearchBox.Text ?? string.Empty).Trim().ToLower();
            IEnumerable<UserRow> data = _allUsers;

            if (!string.IsNullOrEmpty(search))
            {
                data = data.Where(u =>
                    (!string.IsNullOrEmpty(u.Login) && u.Login.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(u.EmployeeName) && u.EmployeeName.ToLower().Contains(search)));
            }

            UsersGrid.ItemsSource = data.ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            LoadUsers();
        }

        private UserRow GetSelectedUser()
        {
            return UsersGrid.SelectedItem as UserRow;
        }

        private void NewUserButton_Click(object sender, RoutedEventArgs e)
        {
            var roleName = _currentUser.Roles?.Name;
            if (!IsOwnerOrAdmin(roleName))
            {
                MessageBox.Show("Создавать пользователей могут только Владелец и Администратор.",
                    "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var wnd = new UserWindow(null);
            wnd.Owner = Window.GetWindow(this);
            if (wnd.ShowDialog() == true)
            {
                LoadUsers();
            }
        }

        private void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            var roleName = _currentUser.Roles?.Name;
            if (!IsOwnerOrAdmin(roleName))
            {
                MessageBox.Show("Редактировать пользователей могут только Владелец и Администратор.",
                    "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = GetSelectedUser();
            if (row == null)
            {
                MessageBox.Show("Выберите пользователя для редактирования.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var wnd = new UserWindow(row.UserId);
            wnd.Owner = Window.GetWindow(this);
            if (wnd.ShowDialog() == true)
            {
                LoadUsers();
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            var roleName = _currentUser.Roles?.Name;
            if (!IsOwnerOrAdmin(roleName))
            {
                MessageBox.Show("Удалять пользователей могут только Владелец и Администратор.",
                    "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = GetSelectedUser();
            if (row == null)
            {
                MessageBox.Show("Выберите пользователя для удаления.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (row.UserId == _currentUser.UserId)
            {
                MessageBox.Show("Нельзя удалить самого себя.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Удалить пользователя \"{row.Login}\"?",
                    "Подтверждение", MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new AtlasServiceCenterEntities())
                {
                    var user = db.Users.FirstOrDefault(u => u.UserId == row.UserId);
                    if (user == null)
                    {
                        MessageBox.Show("Пользователь не найден при удалении.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    db.Users.Remove(user);
                    db.SaveChanges();
                }

                // Удаляем фото, если есть
                try
                {
                    string path = UserPhotoHelper.GetUserPhotoPath(row.UserId);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch
                {
                    // Если файл не удалось удалить, просто игнорируем
                }

                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении пользователя:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UsersGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var row = GetSelectedUser();
            if (row == null)
                return;

            EditUserButton_Click(sender, e);
        }

        private bool IsOwnerOrAdmin(string roleName)
        {
            return RoleHelper.CanManageUsers(roleName);
        }

        public class UserRow
        {
            public int UserId { get; set; }
            public string Login { get; set; }
            public string RoleName { get; set; }
            public string EmployeeName { get; set; }

            // Готовая картинка для биндинга
            public BitmapImage PhotoImage { get; set; }
        }
    }
}
