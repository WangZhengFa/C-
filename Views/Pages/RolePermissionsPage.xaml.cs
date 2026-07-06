using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FoodEnterpriseIMS.Helpers;
using FoodEnterpriseIMS.Services;
using WF = System.Windows.Forms;

namespace 食品信息管理系统.Views.Pages
{
    /// <summary>
    /// 角色与权限管理页（子窗体加载）
    /// </summary>
    public partial class RolePermissionsPage : Page
    {
        public event EventHandler? PermissionsChanged;

        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly ObservableCollection<RoleRow> _roles = new();
        private readonly ObservableCollection<PermissionRow> _permissions = new();
        private readonly ObservableCollection<PermissionTreeNode> _permissionTree = new();
        private bool _isTreeCheckBulkUpdating;
        private readonly WF.TreeView _permissionsTree = new();

        public RolePermissionsPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public RolePermissionsPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;

            RolesGrid.ItemsSource = _roles;
            InitializePermissionsTree();

            ReloadRolesAndPermissions();
            RefreshPermissionsInternal();
            if (_roles.Count > 0)
            {
                RolesGrid.SelectedIndex = 0;
            }

            ApplyButtonPermissions();
        }

        private void InitializePermissionsTree()
        {
            _permissionsTree.BorderStyle = WF.BorderStyle.None;
            _permissionsTree.ShowLines = true;
            _permissionsTree.ShowPlusMinus = true;
            _permissionsTree.ShowRootLines = true;
            _permissionsTree.CheckBoxes = true;
            _permissionsTree.Indent = 18;
            _permissionsTree.ItemHeight = 22;
            _permissionsTree.Font = new Font("Microsoft YaHei", 9f);
            _permissionsTree.AfterCheck += PermissionsTree_AfterCheck;
            PermissionsTreeHost.Child = _permissionsTree;
        }

        private void RefreshRoles_Click(object sender, RoutedEventArgs e)
        {
            ReloadRolesAndPermissions();
        }

        private void AddRole_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var input = ShowRoleEditor(null);
                if (input == null)
                {
                    return;
                }

                _db.AddRole(input.Name, input.Description);
                ReloadRolesAndPermissions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"新增角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditRole_Click(object sender, RoutedEventArgs e)
        {
            if (RolesGrid.SelectedItem is not RoleRow role)
            {
                MessageBox.Show("请先选择角色", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var input = ShowRoleEditor(role);
                if (input == null)
                {
                    return;
                }

                _db.UpdateRole(role.Id, input.Name, input.Description);
                ReloadRolesAndPermissions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"编辑角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteRole_Click(object sender, RoutedEventArgs e)
        {
            if (RolesGrid.SelectedItem is not RoleRow role)
            {
                MessageBox.Show("请先选择角色", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show($"确定删除角色 {role.Name} 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                _db.DeleteRole(role.Id);
                ReloadRolesAndPermissions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SyncPermissions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _db.SyncMenuPermissions();
                RefreshPermissionsInternal();
                LoadRolePermissionsSelection();
                NotifyPermissionsChanged();
                MessageBox.Show("权限同步完成", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"同步权限失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectAllPermissions_Click(object sender, RoutedEventArgs e)
        {
            foreach (var permission in _permissions)
            {
                permission.IsChecked = true;
            }

            RenderPermissionTree();
        }

        private void UnselectAllPermissions_Click(object sender, RoutedEventArgs e)
        {
            foreach (var permission in _permissions)
            {
                permission.IsChecked = false;
            }

            RenderPermissionTree();
        }

        private void SaveRolePermissions_Click(object sender, RoutedEventArgs e)
        {
            if (RolesGrid.SelectedItem is not RoleRow role)
            {
                MessageBox.Show("请先选择角色", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (role.Id == 1)
            {
                MessageBox.Show("系统管理员角色默认拥有全部权限，不允许修改", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var permissionIds = _permissions.Where(p => p.IsChecked).Select(p => p.Id).ToList();
                _db.SaveRolePermissions(role.Id, permissionIds);
                NotifyPermissionsChanged();
                MessageBox.Show("角色权限已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存角色权限失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RolesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadRolePermissionsSelection();
        }

        private void NotifyPermissionsChanged()
        {
            PermissionsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ReloadRolesAndPermissions()
        {
            var selectedRoleId = (RolesGrid.SelectedItem as RoleRow)?.Id;
            RefreshRolesInternal();

            if (selectedRoleId.HasValue)
            {
                var target = _roles.FirstOrDefault(r => r.Id == selectedRoleId.Value);
                if (target != null)
                {
                    RolesGrid.SelectedItem = target;
                }
                else if (_roles.Count > 0)
                {
                    RolesGrid.SelectedIndex = 0;
                }
            }
            else if (_roles.Count > 0)
            {
                RolesGrid.SelectedIndex = 0;
            }

            LoadRolePermissionsSelection();
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

        private void RefreshPermissionsInternal()
        {
            _permissions.Clear();
            foreach (var row in _db.GetMenuButtonPermissions())
            {
                _permissions.Add(new PermissionRow
                {
                    Id = ToLong(row, "id"),
                    PermissionKey = ToStringValue(row, "permission_key"),
                    Name = ToStringValue(row, "name"),
                    NodeType = ToStringValue(row, "node_type"),
                    NodeCode = ToStringValue(row, "node_code"),
                    Description = ToStringValue(row, "description"),
                    IsChecked = false
                });
            }

            RebuildPermissionTree();
        }

        private void LoadRolePermissionsSelection()
        {
            if (RolesGrid.SelectedItem is not RoleRow role)
            {
                foreach (var permission in _permissions)
                {
                    permission.IsChecked = false;
                }

                ApplyAdminRoleLock(null);
                return;
            }

            var selectedIds = _db.GetRolePermissionIds(role.Id).ToHashSet();
            foreach (var permission in _permissions)
            {
                permission.IsChecked = selectedIds.Contains(permission.Id);
            }

            ApplyAdminRoleLock(role);
        }

        private void ApplyAdminRoleLock(RoleRow? role)
        {
            var isAdminRole = role != null && role.Id == 1;
            _permissionsTree.Enabled = !isAdminRole;
            BtnSelectAllPermissions.IsEnabled = !isAdminRole;
            BtnUnselectAllPermissions.IsEnabled = !isAdminRole;
            BtnSaveRolePermissions.IsEnabled = !isAdminRole;

            if (!isAdminRole)
            {
                return;
            }

            foreach (var permission in _permissions)
            {
                permission.IsChecked = true;
            }

            RenderPermissionTree();
        }

        private void RebuildPermissionTree()
        {
            _permissionTree.Clear();

            var allMenus = _db.GetMenuList();
            var menuMeta = allMenus
                .Select(row => new
                {
                    Code = ToStringValue(row, "menu_key"),
                    ParentCode = ToStringValue(row, "parent_key"),
                    Title = ToStringValue(row, "title"),
                    SortOrder = ToInt(row, "sort_order")
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                .ToDictionary(x => x.Code, x => x, StringComparer.OrdinalIgnoreCase);

            var menuPermissions = _permissions
                .Where(p => string.Equals(p.NodeType, "menu", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var buttonPermissions = _permissions
                .Where(p => string.Equals(p.NodeType, "button", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var nodeMap = new Dictionary<string, PermissionTreeNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var permission in menuPermissions)
            {
                var nodeCode = permission.NodeCode;
                var title = permission.Name;
                var sortOrder = int.MaxValue;
                var parentCode = string.Empty;

                if (!string.IsNullOrWhiteSpace(nodeCode) && menuMeta.TryGetValue(nodeCode, out var meta))
                {
                    title = string.IsNullOrWhiteSpace(meta.Title) ? title : meta.Title;
                    parentCode = meta.ParentCode;
                    sortOrder = meta.SortOrder;
                }

                nodeMap[nodeCode] = new PermissionTreeNode
                {
                    Permission = permission,
                    DisplayName = string.IsNullOrWhiteSpace(title) ? permission.PermissionKey : title,
                    DisplayType = "menu",
                    NodeCode = nodeCode,
                    ParentCode = parentCode,
                    SortOrder = sortOrder
                };
            }

            foreach (var menuNode in nodeMap.Values)
            {
                if (!string.IsNullOrWhiteSpace(menuNode.ParentCode)
                    && nodeMap.TryGetValue(menuNode.ParentCode, out var parent))
                {
                    parent.Children.Add(menuNode);
                }
                else
                {
                    _permissionTree.Add(menuNode);
                }
            }

            foreach (var button in buttonPermissions)
            {
                var leaf = new PermissionTreeNode
                {
                    Permission = button,
                    DisplayName = string.IsNullOrWhiteSpace(button.Name) ? button.PermissionKey : button.Name,
                    DisplayType = "button",
                    NodeCode = button.NodeCode,
                    ParentCode = button.NodeCode,
                    SortOrder = int.MaxValue
                };

                if (!string.IsNullOrWhiteSpace(button.NodeCode) && nodeMap.TryGetValue(button.NodeCode, out var parent))
                {
                    parent.Children.Add(leaf);
                }
                else
                {
                    _permissionTree.Add(leaf);
                }
            }

            SortPermissionTree(_permissionTree);
            RenderPermissionTree();
        }

        private void RenderPermissionTree()
        {
            _isTreeCheckBulkUpdating = true;
            _permissionsTree.BeginUpdate();
            try
            {
                _permissionsTree.Nodes.Clear();
                foreach (var node in _permissionTree)
                {
                    _permissionsTree.Nodes.Add(BuildWinFormsPermissionNode(node));
                }

                _permissionsTree.ExpandAll();
            }
            finally
            {
                _permissionsTree.EndUpdate();
                _isTreeCheckBulkUpdating = false;
            }
        }

        private WF.TreeNode BuildWinFormsPermissionNode(PermissionTreeNode node)
        {
            var key = node.Permission?.PermissionKey ?? string.Empty;
            var text = string.IsNullOrWhiteSpace(key)
                ? $"{node.DisplayName} ({node.DisplayType})"
                : $"{node.DisplayName}    [{key}]    ({node.DisplayType})";

            var uiNode = new WF.TreeNode
            {
                Text = text,
                Tag = node,
                Checked = node.Permission?.IsChecked ?? false
            };

            foreach (var child in node.Children)
            {
                uiNode.Nodes.Add(BuildWinFormsPermissionNode(child));
            }

            return uiNode;
        }

        private void PermissionsTree_AfterCheck(object? sender, WF.TreeViewEventArgs e)
        {
            if (_isTreeCheckBulkUpdating)
            {
                return;
            }

            _isTreeCheckBulkUpdating = true;
            try
            {
                ApplyNodeCheckState(e.Node, e.Node.Checked);
            }
            finally
            {
                _isTreeCheckBulkUpdating = false;
            }
        }

        private static void ApplyNodeCheckState(WF.TreeNode node, bool isChecked)
        {
            if (node.Tag is PermissionTreeNode model && model.Permission != null)
            {
                model.Permission.IsChecked = isChecked;
            }

            foreach (WF.TreeNode child in node.Nodes)
            {
                child.Checked = isChecked;
                ApplyNodeCheckState(child, isChecked);
            }
        }

        private static void SortPermissionTree(ObservableCollection<PermissionTreeNode> nodes)
        {
            var sorted = nodes
                .OrderBy(n => n.DisplayType == "button" ? 1 : 0)
                .ThenBy(n => n.SortOrder)
                .ThenBy(n => n.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            nodes.Clear();
            foreach (var node in sorted)
            {
                nodes.Add(node);
                if (node.Children.Count > 0)
                {
                    SortPermissionTree(node.Children);
                }
            }
        }


        private void ApplyButtonPermissions()
        {
            try
            {
                PagePermissionHelper.ApplyButtonPermissions(this, "role_permissions", _currentRole, _db);
            }
            catch
            {
                // 权限应用失败不阻塞页面使用
            }
        }

        private RoleEditInput? ShowRoleEditor(RoleRow? existing)
        {
            var dialog = new Window
            {
                Title = existing == null ? "新增角色" : "编辑角色",
                Width = 420,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize
            };

            var root = new Grid { Margin = new Thickness(12) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var nameLabel = new TextBlock { Text = "角色名", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 6, 8, 6) };
            Grid.SetRow(nameLabel, 0);
            root.Children.Add(nameLabel);

            var nameBox = new TextBox { Text = existing?.Name ?? string.Empty, Margin = new Thickness(0, 4, 0, 4) };
            Grid.SetRow(nameBox, 0);
            Grid.SetColumn(nameBox, 1);
            root.Children.Add(nameBox);

            var descLabel = new TextBlock { Text = "描述", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 6, 8, 6) };
            Grid.SetRow(descLabel, 1);
            root.Children.Add(descLabel);

            var descBox = new TextBox { Text = existing?.Description ?? string.Empty, Margin = new Thickness(0, 4, 0, 4) };
            Grid.SetRow(descBox, 1);
            Grid.SetColumn(descBox, 1);
            root.Children.Add(descBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okBtn = new Button { Content = "保存", Width = 88, Margin = new Thickness(0, 8, 8, 0), IsDefault = true };
            var cancelBtn = new Button { Content = "取消", Width = 88, Margin = new Thickness(0, 8, 0, 0), IsCancel = true };
            buttonPanel.Children.Add(okBtn);
            buttonPanel.Children.Add(cancelBtn);
            Grid.SetRow(buttonPanel, 3);
            Grid.SetColumn(buttonPanel, 1);
            root.Children.Add(buttonPanel);

            dialog.Content = root;

            RoleEditInput? result = null;
            okBtn.Click += (_, _) =>
            {
                var name = nameBox.Text.Trim();
                var description = descBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show(dialog, "角色名不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                result = new RoleEditInput
                {
                    Name = name,
                    Description = description
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

        private sealed class RoleRow
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        private sealed class PermissionRow : INotifyPropertyChanged
        {
            private bool _isChecked;

            public long Id { get; set; }
            public string PermissionKey { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string NodeType { get; set; } = string.Empty;
            public string NodeCode { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;

            public bool IsChecked
            {
                get => _isChecked;
                set
                {
                    if (_isChecked == value)
                    {
                        return;
                    }

                    _isChecked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
        }

        private sealed class PermissionTreeNode
        {
            public PermissionRow? Permission { get; set; }
            public string DisplayName { get; set; } = string.Empty;
            public string DisplayType { get; set; } = string.Empty;
            public string NodeCode { get; set; } = string.Empty;
            public string ParentCode { get; set; } = string.Empty;
            public int SortOrder { get; set; }
            public bool CanCheck => Permission != null;
            public ObservableCollection<PermissionTreeNode> Children { get; } = new();
        }

        private sealed class RoleEditInput
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }
    }
}
