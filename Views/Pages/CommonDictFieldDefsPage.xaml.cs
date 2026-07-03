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
    /// 通用字典字段定义页面
    /// </summary>
    public partial class CommonDictFieldDefsPage : Page
    {
        public event EventHandler? CloseRequested;

        private readonly CommonDictFieldDefsService _service;
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly ObservableCollection<CommonDictFieldDefsRecord> _records = new();
        private readonly ICollectionView _recordView;

        public CommonDictFieldDefsPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public CommonDictFieldDefsPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            _service = new CommonDictFieldDefsService();

            _recordView = CollectionViewSource.GetDefaultView(_records);
            _recordView.Filter = RecordFilter;
            RecordGrid.ItemsSource = _recordView;

            InitFilterOptions();
            ApplyButtonPermissions();
            LoadRecords();
        }

        private void InitFilterOptions()
        {
            FieldTypeFilterCombo.Items.Clear();
            FieldTypeFilterCombo.Items.Add(string.Empty);
            FieldTypeFilterCombo.Items.Add("text");
            FieldTypeFilterCombo.Items.Add("number");
            FieldTypeFilterCombo.Items.Add("date");
            FieldTypeFilterCombo.Items.Add("bool");
            FieldTypeFilterCombo.Items.Add("select");
        }

        private void LoadRecords()
        {
            _records.Clear();
            foreach (var item in _service.ListAll())
            {
                _records.Add(item);
            }

            var currentType = FieldTypeFilterCombo.Text?.Trim() ?? string.Empty;
            var types = _records.Select(x => x.FieldType)
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .Distinct()
                                .OrderBy(x => x)
                                .ToList();
            InitFilterOptions();
            foreach (var type in types)
            {
                if (!FieldTypeFilterCombo.Items.Contains(type))
                {
                    FieldTypeFilterCombo.Items.Add(type);
                }
            }
            FieldTypeFilterCombo.Text = currentType;

            _recordView.Refresh();
        }

        private bool RecordFilter(object item)
        {
            if (item is not CommonDictFieldDefsRecord record)
            {
                return false;
            }

            var keyword = KeywordFilterText.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var hit = Contains(record.FieldKey, keyword)
                          || Contains(record.FieldLabel, keyword)
                          || Contains(record.FieldType, keyword)
                          || Contains(record.Placeholder, keyword)
                          || Contains(record.Options, keyword)
                          || Contains(record.Description, keyword)
                          || Contains(record.NodeCode, keyword);
                if (!hit)
                {
                    return false;
                }
            }

            var fieldType = FieldTypeFilterCombo.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(fieldType) && !string.Equals(record.FieldType ?? string.Empty, fieldType, StringComparison.OrdinalIgnoreCase))
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
            var dialog = new CommonDictFieldDefsEditWindow(null, _records) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"新增字段定义失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not CommonDictFieldDefsRecord selected)
            {
                MessageBox.Show("请选择一条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new CommonDictFieldDefsEditWindow(selected, _records) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"更新字段定义失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not CommonDictFieldDefsRecord selected)
            {
                MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"确认删除字段定义 [{selected.FieldKey}] 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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
                MessageBox.Show($"删除字段定义失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            FieldTypeFilterCombo.Text = string.Empty;
            EnabledOnlyCheck.IsChecked = false;
            _recordView.Refresh();
        }

        private void ApplyButtonPermissions()
        {
            try
            {
                PagePermissionHelper.ApplyButtonPermissions(this, "common_dict_field_defs", _currentRole, _db);
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
