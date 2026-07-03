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
    /// 客户信息页面
    /// </summary>
    public partial class CustomerInfoPage : Page
    {
        public event EventHandler? CloseRequested;

        private readonly CustomerInfoService _service;
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly ObservableCollection<CustomerInfoRecord> _records = new();
        private readonly ICollectionView _recordView;

        public CustomerInfoPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public CustomerInfoPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            _service = new CustomerInfoService();

            _recordView = CollectionViewSource.GetDefaultView(_records);
            _recordView.Filter = RecordFilter;
            RecordGrid.ItemsSource = _recordView;

            InitFilterOptions();
            ApplyButtonPermissions();
            LoadRecords();
        }

        private void InitFilterOptions()
        {
            SourceFilterCombo.Items.Clear();
            SourceFilterCombo.Items.Add(string.Empty);
            SourceFilterCombo.Items.Add("客户录入");
            SourceFilterCombo.Items.Add("导入");
            SourceFilterCombo.Items.Add("系统同步");

            TypeFilterCombo.Items.Clear();
            TypeFilterCombo.Items.Add(string.Empty);
            TypeFilterCombo.Items.Add("生产企业");
            TypeFilterCombo.Items.Add("经营企业");
            TypeFilterCombo.Items.Add("个体工商户");
        }

        private void LoadRecords()
        {
            _records.Clear();
            foreach (var item in _service.ListAll())
            {
                _records.Add(item);
            }

            var currentSource = SourceFilterCombo.Text?.Trim() ?? string.Empty;
            var currentType = TypeFilterCombo.Text?.Trim() ?? string.Empty;
            var sources = _records.Select(x => x.Source)
                                  .Where(x => !string.IsNullOrWhiteSpace(x))
                                  .Distinct()
                                  .OrderBy(x => x)
                                  .ToList();
            var types = _records.Select(x => x.CustomerType)
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .Distinct()
                                .OrderBy(x => x)
                                .ToList();
            InitFilterOptions();
            foreach (var value in sources)
            {
                if (!SourceFilterCombo.Items.Contains(value))
                {
                    SourceFilterCombo.Items.Add(value);
                }
            }
            foreach (var value in types)
            {
                if (!TypeFilterCombo.Items.Contains(value))
                {
                    TypeFilterCombo.Items.Add(value);
                }
            }
            SourceFilterCombo.Text = currentSource;
            TypeFilterCombo.Text = currentType;

            _recordView.Refresh();
        }

        private bool RecordFilter(object item)
        {
            if (item is not CustomerInfoRecord record)
            {
                return false;
            }

            var keyword = KeywordFilterText.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var hit = Contains(record.CustomerId, keyword)
                          || Contains(record.CustomerName, keyword)
                          || Contains(record.CustomerType, keyword)
                          || Contains(record.LicenseNo, keyword)
                          || Contains(record.BusinessLicense, keyword)
                          || Contains(record.ContactAddress, keyword)
                          || Contains(record.ContactPerson, keyword)
                          || Contains(record.ContactPhone, keyword)
                          || Contains(record.Remark, keyword);
                if (!hit)
                {
                    return false;
                }
            }

            var source = SourceFilterCombo.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(source) && !string.Equals(record.Source ?? string.Empty, source, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var type = TypeFilterCombo.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(type) && !string.Equals(record.CustomerType ?? string.Empty, type, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (DisabledOnlyCheck.IsChecked == true && !record.IsDisabled)
            {
                return false;
            }

            return true;
        }

        private static bool Contains(string? text, string keyword)
        {
            return (text ?? string.Empty).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomerInfoEditWindow(null, _records) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _service.Insert(dialog.Value);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"新增客户信息失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not CustomerInfoRecord selected)
            {
                MessageBox.Show("请选择一条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new CustomerInfoEditWindow(selected, _records) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                dialog.Value.Id = selected.Id;
                _service.Update(dialog.Value);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新客户信息失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not CustomerInfoRecord selected)
            {
                MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"确认删除客户 [{selected.CustomerName}] 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                _service.Delete(selected.Id);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除客户信息失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            SourceFilterCombo.Text = string.Empty;
            TypeFilterCombo.Text = string.Empty;
            DisabledOnlyCheck.IsChecked = false;
            _recordView.Refresh();
        }

        private void ApplyButtonPermissions()
        {
            try
            {
                PagePermissionHelper.ApplyButtonPermissions(this, "customer_info", _currentRole, _db);
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
