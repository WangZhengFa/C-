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
    /// 营养标签页面
    /// </summary>
    public partial class NutritionLabelPage : Page
    {
        public event EventHandler? CloseRequested;

        private readonly NutritionLabelService _service;
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly ObservableCollection<NutritionLabelRecord> _records = new();
        private readonly ICollectionView _recordView;

        public NutritionLabelPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public NutritionLabelPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            _service = new NutritionLabelService();

            _recordView = CollectionViewSource.GetDefaultView(_records);
            _recordView.Filter = RecordFilter;
            RecordGrid.ItemsSource = _recordView;

            InitFilterOptions();
            ApplyButtonPermissions();
            LoadRecords();
        }

        private void InitFilterOptions()
        {
            DetectionModeFilterCombo.Items.Clear();
            DetectionModeFilterCombo.Items.Add(string.Empty);
            DetectionModeFilterCombo.Items.Add("core");
            DetectionModeFilterCombo.Items.Add("extended");
        }

        private void LoadRecords()
        {
            _records.Clear();
            foreach (var item in _service.ListAll())
            {
                _records.Add(item);
            }

            var current = DetectionModeFilterCombo.Text?.Trim() ?? string.Empty;
            var values = _records.Select(x => x.DetectionMode)
                                 .Where(x => !string.IsNullOrWhiteSpace(x))
                                 .Distinct()
                                 .OrderBy(x => x)
                                 .ToList();
            InitFilterOptions();
            foreach (var value in values)
            {
                if (!DetectionModeFilterCombo.Items.Contains(value))
                {
                    DetectionModeFilterCombo.Items.Add(value);
                }
            }
            DetectionModeFilterCombo.Text = current;

            _recordView.Refresh();
        }

        private bool RecordFilter(object item)
        {
            if (item is not NutritionLabelRecord record)
            {
                return false;
            }

            var keyword = KeywordFilterText.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var hit = Contains(record.LabelId, keyword)
                          || Contains(record.Name, keyword)
                          || Contains(record.ClaimTypes, keyword)
                          || Contains(record.Remark, keyword);
                if (!hit)
                {
                    return false;
                }
            }

            var mode = DetectionModeFilterCombo.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(mode) && !string.Equals(record.DetectionMode ?? string.Empty, mode, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (HeavyMetalOnlyCheck.IsChecked == true && !record.HeavyMetal)
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
            var dialog = new NutritionLabelEditWindow(null, _records) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"新增营养标签失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not NutritionLabelRecord selected)
            {
                MessageBox.Show("请选择一条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new NutritionLabelEditWindow(selected, _records) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _service.Update(dialog.Value);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新营养标签失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not NutritionLabelRecord selected)
            {
                MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"确认删除营养标签 [{selected.LabelId}] 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                _service.Delete(selected.LabelId);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除营养标签失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            DetectionModeFilterCombo.Text = string.Empty;
            HeavyMetalOnlyCheck.IsChecked = false;
            _recordView.Refresh();
        }

        private void ApplyButtonPermissions()
        {
            try
            {
                PagePermissionHelper.ApplyButtonPermissions(this, "nutrition_label", _currentRole, _db);
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
