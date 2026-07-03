using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using FoodEnterpriseIMS.Helpers;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;
using 食品信息管理系统.Views.Dialogs;

namespace 食品信息管理系统.Views.Pages
{
    /// <summary>
    /// 系统配置页面
    /// </summary>
    public partial class SystemConfigPage : Page
    {
        public event EventHandler? CloseRequested;

        private readonly SystemConfigService _service;
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly ObservableCollection<SystemConfigRecord> _records = new();
        private readonly ICollectionView _recordView;

        public SystemConfigPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public SystemConfigPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            _service = new SystemConfigService();

            _recordView = CollectionViewSource.GetDefaultView(_records);
            _recordView.Filter = RecordFilter;
            RecordGrid.ItemsSource = _recordView;

            ApplyButtonPermissions();
            LoadRecords();
        }

        private void LoadRecords()
        {
            _records.Clear();
            foreach (var item in _service.ListAll())
            {
                _records.Add(item);
            }

            _recordView.Refresh();
        }

        private bool RecordFilter(object item)
        {
            if (item is not SystemConfigRecord record)
            {
                return false;
            }

            var keyword = KeywordFilterText.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                if (!Contains(record.ConfigKey, keyword) && !Contains(record.Value, keyword))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Contains(string? text, string keyword)
        {
            return (text ?? string.Empty).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SystemConfigEditWindow(null, _records) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _service.Upsert(dialog.Value);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"新增系统配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not SystemConfigRecord selected)
            {
                MessageBox.Show("请选择一条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SystemConfigEditWindow(selected, _records) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _service.Upsert(dialog.Value);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新系统配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not SystemConfigRecord selected)
            {
                MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"确认删除配置键 [{selected.ConfigKey}] 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                _service.Delete(selected.ConfigKey);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除系统配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRecords();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnFilterChanged(object sender, RoutedEventArgs e)
        {
            _recordView.Refresh();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            KeywordFilterText.Text = string.Empty;
            _recordView.Refresh();
        }

        private void ApplyButtonPermissions()
        {
            try
            {
                PagePermissionHelper.ApplyButtonPermissions(this, "system_config", _currentRole, _db);
            }
            catch
            {
                // ignore
            }
        }

        public void RefreshPermissionState()
        {
            ApplyButtonPermissions();
        }
    }
}
