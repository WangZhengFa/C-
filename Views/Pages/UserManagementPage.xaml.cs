using System;
using System.Windows;
using System.Windows.Controls;
using FoodEnterpriseIMS.Helpers;
using FoodEnterpriseIMS.Services;
using 食品信息管理系统.Views;

namespace 食品信息管理系统.Views.Pages
{
    /// <summary>
    /// 用户管理入口页
    /// </summary>
    public partial class UserManagementPage : Page
    {
        public event EventHandler? CloseRequested;

        private readonly DatabaseManager _db;
        private readonly int _currentRole;

        public UserManagementPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public UserManagementPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
        }

        private void OpenWindow_Click(object sender, RoutedEventArgs e)
        {
            var window = new UserManagementWindow(_db, _currentRole)
            {
                Owner = Window.GetWindow(this)
            };
            window.PermissionsChanged += (_, _) => { };
            window.ShowDialog();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("用户管理入口页不保存数据，直接打开窗口即可完成管理。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
