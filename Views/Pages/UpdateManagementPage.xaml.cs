using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using FoodEnterpriseIMS.Helpers;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;

namespace 食品信息管理系统.Views.Pages
{
    /// <summary>
    /// 更新管理页面
    /// </summary>
    public partial class UpdateManagementPage : Page
    {
        public event EventHandler? CloseRequested;

        private readonly UpdateManagementService _service;
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly ObservableCollection<UpdateManagementSettingRecord> _records = new();

        public UpdateManagementPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public UpdateManagementPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            _service = new UpdateManagementService();
            RecordGrid.ItemsSource = _records;

            LoadRecords();
        }

        private void LoadRecords()
        {
            _records.Clear();
            foreach (var item in _service.ListAll(_db))
            {
                _records.Add(item);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRecords();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _service.SaveAll(_db, _records);
                MessageBox.Show("更新管理配置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存更新管理配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
