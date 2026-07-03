using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
    /// 通用字典页面
    /// </summary>
    public partial class GeneralDictionaryPage : Page
    {
        public event EventHandler? CloseRequested;

        private readonly CommonDictDataService _service;
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly ObservableCollection<CommonDictDataRecord> _records = new();
        private readonly ICollectionView _recordView;

        public GeneralDictionaryPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public GeneralDictionaryPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            _service = new CommonDictDataService();

            _recordView = CollectionViewSource.GetDefaultView(_records);
            _recordView.Filter = RecordFilter;
            RecordGrid.ItemsSource = _recordView;

            InitFilterOptions();
            ApplyButtonPermissions();
            LoadRecords();
        }

        private void InitFilterOptions()
        {
            NodeCodeFilterCombo.Items.Clear();
            NodeCodeFilterCombo.Items.Add(string.Empty);
        }

        private void LoadRecords()
        {
            _records.Clear();
            foreach (var item in _service.ListAll())
            {
                _records.Add(item);
            }

            var currentNodeCode = NodeCodeFilterCombo.Text?.Trim() ?? string.Empty;
            var values = _records.Select(x => x.NodeCode)
                                 .Where(x => !string.IsNullOrWhiteSpace(x))
                                 .Distinct()
                                 .OrderBy(x => x)
                                 .ToList();
            InitFilterOptions();
            foreach (var value in values)
            {
                if (!NodeCodeFilterCombo.Items.Contains(value))
                {
                    NodeCodeFilterCombo.Items.Add(value);
                }
            }
            NodeCodeFilterCombo.Text = currentNodeCode;

            _recordView.Refresh();
        }

        private bool RecordFilter(object item)
        {
            if (item is not CommonDictDataRecord record)
            {
                return false;
            }

            var keyword = KeywordFilterText.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var hit = Contains(record.NodeCode, keyword)
                          || Contains(record.Code, keyword)
                          || Contains(record.Name, keyword)
                          || Contains(record.Field1, keyword)
                          || Contains(record.Field2, keyword)
                          || Contains(record.Field3, keyword)
                          || Contains(record.Field4, keyword)
                          || Contains(record.Field5, keyword)
                          || Contains(record.Remark, keyword);
                if (!hit)
                {
                    return false;
                }
            }

            var nodeCode = NodeCodeFilterCombo.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(nodeCode) && !string.Equals(record.NodeCode ?? string.Empty, nodeCode, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (EnabledOnlyCheck.IsChecked == true && !record.IsEnabled)
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
            var dialog = new CommonDictDataEditWindow(null, _records) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"新增通用字典数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not CommonDictDataRecord selected)
            {
                MessageBox.Show("请选择一条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new CommonDictDataEditWindow(selected, _records) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"更新通用字典数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not CommonDictDataRecord selected)
            {
                MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"确认删除字典数据 [{selected.Code}] 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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
                MessageBox.Show($"删除通用字典数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            NodeCodeFilterCombo.Text = string.Empty;
            EnabledOnlyCheck.IsChecked = false;
            _recordView.Refresh();
        }

        private void ApplyButtonPermissions()
        {
            try
            {
                PagePermissionHelper.ApplyButtonPermissions(this, "general_dictionary", _currentRole, _db);
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
