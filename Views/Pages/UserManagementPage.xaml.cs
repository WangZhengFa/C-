using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FoodEnterpriseIMS.Helpers;
using FoodEnterpriseIMS.Services;

namespace 食品信息管理系统.Views.Pages
{
    /// <summary>
    /// 用户管理页（子窗体加载）
    /// </summary>
    public partial class UserManagementPage : Page
    {
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly ObservableCollection<UserRow> _users = new();
        private readonly ObservableCollection<RoleRow> _roles = new();

        public UserManagementPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public UserManagementPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;

            UsersGrid.ItemsSource = _users;
            RefreshUsersInternal();
            RefreshRolesInternal();
            ApplyButtonPermissions();
        }

        private void RefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            RefreshUsersInternal();
            RefreshRolesInternal();
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var input = ShowUserEditor(null);
                if (input == null)
                {
                    return;
                }

                _db.AddUser(input.Username, input.Password, input.RoleId, input.Nickname, input.Department);
                RefreshUsersInternal();
                RefreshRolesInternal();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"新增用户失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is not UserRow user)
            {
                MessageBox.Show("请先选择用户", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var input = ShowUserEditor(user);
                if (input == null)
                {
                    return;
                }

                _db.UpdateUser(user.Id, input.Nickname, input.Department, input.RoleId, input.IsDisabled, string.IsNullOrWhiteSpace(input.Password) ? null : input.Password);
                RefreshUsersInternal();
                RefreshRolesInternal();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"编辑用户失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is not UserRow user)
            {
                MessageBox.Show("请先选择用户", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show($"确定删除用户 {user.Username} 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                _db.DeleteUser(user.Id);
                RefreshUsersInternal();
                RefreshRolesInternal();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除用户失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshUsersInternal()
        {
            _users.Clear();
            foreach (var row in _db.GetAllUsers())
            {
                _users.Add(new UserRow
                {
                    Id = ToLong(row, "id"),
                    Username = ToStringValue(row, "username"),
                    Nickname = ToStringValue(row, "nickname"),
                    Department = ToStringValue(row, "department"),
                    RoleId = ToLong(row, "role_id"),
                    RoleName = ToStringValue(row, "role_name"),
                    IsDisabled = ToInt(row, "is_disabled") != 0,
                    LastLogin = ToStringValue(row, "last_login"),
                    LoginCountMonth = ToInt(row, "login_count_month"),
                    LoginCountTotal = ToInt(row, "login_count_total")
                });
            }
        }

        private void RefreshRolesInternal()
        {
            _roles.Clear();
            foreach (var row in _db.GetAllRoles())
            {
                _roles.Add(new RoleRow
                {
                    Id = ToLong(row, "id"),
                    Name = ToStringValue(row, "name"),
                    Description = ToStringValue(row, "description")
                });
            }
        }

        private void ApplyButtonPermissions()
        {
            try
            {
                PagePermissionHelper.ApplyButtonPermissions(this, "user_management", _currentRole, _db);
            }
            catch
            {
                // 权限应用失败不阻塞页面使用
            }
        }

        private UserEditInput? ShowUserEditor(UserRow? existing)
        {
            var dialog = new Window
            {
                Title = existing == null ? "新增用户" : "编辑用户",
                Width = 420,
                Height = 360,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize
            };

            var root = new Grid { Margin = new Thickness(12) };
            for (var i = 0; i < 7; i++)
            {
                root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            void AddLabel(string text, int row)
            {
                var label = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 6, 8, 6) };
                Grid.SetRow(label, row);
                Grid.SetColumn(label, 0);
                root.Children.Add(label);
            }

            AddLabel("用户名", 0);
            var usernameBox = new TextBox { Text = existing?.Username ?? string.Empty, Margin = new Thickness(0, 4, 0, 4), IsEnabled = existing == null };
            Grid.SetRow(usernameBox, 0);
            Grid.SetColumn(usernameBox, 1);
            root.Children.Add(usernameBox);

            AddLabel(existing == null ? "密码" : "新密码", 1);
            var passwordBox = new PasswordBox { Margin = new Thickness(0, 4, 0, 4) };
            if (existing == null)
            {
                passwordBox.Password = "123456";
            }

            Grid.SetRow(passwordBox, 1);
            Grid.SetColumn(passwordBox, 1);
            root.Children.Add(passwordBox);

            AddLabel("昵称", 2);
            var nicknameBox = new TextBox { Text = existing?.Nickname ?? string.Empty, Margin = new Thickness(0, 4, 0, 4) };
            Grid.SetRow(nicknameBox, 2);
            Grid.SetColumn(nicknameBox, 1);
            root.Children.Add(nicknameBox);

            AddLabel("部门", 3);
            var departmentCombo = new ComboBox
            {
                Margin = new Thickness(0, 4, 0, 4),
                IsEditable = true,
                IsTextSearchEnabled = true
            };
            var departments = _users.Select(u => u.Department)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct()
                .OrderBy(d => d)
                .ToList();
            foreach (var dept in departments)
            {
                departmentCombo.Items.Add(dept);
            }

            departmentCombo.Text = existing?.Department ?? string.Empty;
            Grid.SetRow(departmentCombo, 3);
            Grid.SetColumn(departmentCombo, 1);
            root.Children.Add(departmentCombo);

            AddLabel("角色", 4);
            var roleCombo = new ComboBox { Margin = new Thickness(0, 4, 0, 4), DisplayMemberPath = "Name", SelectedValuePath = "Id" };
            foreach (var role in _roles)
            {
                roleCombo.Items.Add(role);
            }

            roleCombo.SelectedValue = existing?.RoleId ?? 2L;
            Grid.SetRow(roleCombo, 4);
            Grid.SetColumn(roleCombo, 1);
            root.Children.Add(roleCombo);

            AddLabel("状态", 5);
            var disabledCheck = new CheckBox { Content = "禁用账号", IsChecked = existing?.IsDisabled ?? false, Margin = new Thickness(0, 4, 0, 4) };
            Grid.SetRow(disabledCheck, 5);
            Grid.SetColumn(disabledCheck, 1);
            root.Children.Add(disabledCheck);

            var tips = new TextBlock
            {
                Text = existing == null ? "新增用户将使用输入密码创建账号。" : "编辑用户时密码留空表示不修改。",
                Margin = new Thickness(0, 6, 0, 4),
                Foreground = System.Windows.Media.Brushes.DimGray
            };
            Grid.SetRow(tips, 6);
            Grid.SetColumn(tips, 1);
            root.Children.Add(tips);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okBtn = new Button { Content = "保存", Width = 88, Margin = new Thickness(0, 8, 8, 0), IsDefault = true };
            var cancelBtn = new Button { Content = "取消", Width = 88, Margin = new Thickness(0, 8, 0, 0), IsCancel = true };
            buttonPanel.Children.Add(okBtn);
            buttonPanel.Children.Add(cancelBtn);
            Grid.SetRow(buttonPanel, 8);
            Grid.SetColumn(buttonPanel, 1);
            root.Children.Add(buttonPanel);

            dialog.Content = root;

            UserEditInput? result = null;
            okBtn.Click += (_, _) =>
            {
                var username = usernameBox.Text.Trim();
                var password = passwordBox.Password.Trim();
                var nickname = nicknameBox.Text.Trim();
                var department = departmentCombo.Text.Trim();
                var roleId = roleCombo.SelectedValue is long v ? v : 2L;
                var isDisabled = disabledCheck.IsChecked == true;

                if (string.IsNullOrWhiteSpace(username))
                {
                    MessageBox.Show(dialog, "用户名不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (existing == null && string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show(dialog, "密码不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                result = new UserEditInput
                {
                    Username = username,
                    Password = password,
                    Nickname = nickname,
                    Department = department,
                    RoleId = roleId,
                    IsDisabled = isDisabled
                };

                dialog.DialogResult = true;
                dialog.Close();
            };

            return dialog.ShowDialog() == true ? result : null;
        }

        private static string ToStringValue(Dictionary<string, object> row, string key)
        {
            return row.TryGetValue(key, out var value) && value != null ? value.ToString() ?? string.Empty : string.Empty;
        }

        private static int ToInt(Dictionary<string, object> row, string key)
        {
            return int.TryParse(ToStringValue(row, key), out var value) ? value : 0;
        }

        private static long ToLong(Dictionary<string, object> row, string key)
        {
            return long.TryParse(ToStringValue(row, key), out var value) ? value : 0;
        }

        private sealed class UserRow
        {
            public long Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Nickname { get; set; } = string.Empty;
            public string Department { get; set; } = string.Empty;
            public long RoleId { get; set; }
            public string RoleName { get; set; } = string.Empty;
            public bool IsDisabled { get; set; }
            public string LastLogin { get; set; } = string.Empty;
            public int LoginCountMonth { get; set; }
            public int LoginCountTotal { get; set; }
            public string StatusText => IsDisabled ? "禁用" : "正常";
        }

        private sealed class RoleRow
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        private sealed class UserEditInput
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Nickname { get; set; } = string.Empty;
            public string Department { get; set; } = string.Empty;
            public long RoleId { get; set; }
            public bool IsDisabled { get; set; }
        }
    }
}
