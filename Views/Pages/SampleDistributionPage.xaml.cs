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
    /// 样品分发页面
    /// </summary>
    public partial class SampleDistributionPage : Page
    {
        public event EventHandler? CloseRequested;

        private readonly SampleDistributionService _service;
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly ObservableCollection<SampleDistributionRecord> _records = new();
        private readonly ICollectionView _recordView;

        public SampleDistributionPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public SampleDistributionPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            _service = new SampleDistributionService();

            _recordView = CollectionViewSource.GetDefaultView(_records);
            _recordView.Filter = RecordFilter;
            RecordGrid.ItemsSource = _recordView;

            InitFilterOptions();
            ApplyButtonPermissions();
            LoadRecords();
        }

        private void InitFilterOptions()
        {
            SampleSourceFilterCombo.Items.Clear();
            SampleSourceFilterCombo.Items.Add(string.Empty);
        }

        private void LoadRecords()
        {
            _records.Clear();
            foreach (var item in _service.ListAll())
            {
                _records.Add(item);
            }

            var current = SampleSourceFilterCombo.Text?.Trim() ?? string.Empty;
            var sources = _records.Select(x => x.SampleSource)
                                  .Where(x => !string.IsNullOrWhiteSpace(x))
                                  .Distinct()
                                  .OrderBy(x => x)
                                  .ToList();
            InitFilterOptions();
            foreach (var source in sources)
            {
                if (!SampleSourceFilterCombo.Items.Contains(source))
                {
                    SampleSourceFilterCombo.Items.Add(source);
                }
            }
            SampleSourceFilterCombo.Text = current;

            _recordView.Refresh();
        }

        private bool RecordFilter(object item)
        {
            if (item is not SampleDistributionRecord record)
            {
                return false;
            }

            var keyword = KeywordFilterText.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var hit = Contains(record.ReceiveSendId, keyword)
                          || Contains(record.SampleName, keyword)
                          || Contains(record.SampleBatch, keyword)
                          || Contains(record.SampleSource, keyword)
                          || Contains(record.Remark, keyword);
                if (!hit)
                {
                    return false;
                }
            }

            var source = SampleSourceFilterCombo.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(source) && !string.Equals(record.SampleSource ?? string.Empty, source, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var from = DateFromPicker.SelectedDate;
            if (from.HasValue && (!record.ReceiveSendDate.HasValue || record.ReceiveSendDate.Value.Date < from.Value.Date))
            {
                return false;
            }

            var to = DateToPicker.SelectedDate;
            if (to.HasValue && (!record.ReceiveSendDate.HasValue || record.ReceiveSendDate.Value.Date > to.Value.Date))
            {
                return false;
            }

            if (OnlyReinspectionCheck.IsChecked == true && !record.IsReinspection)
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
            var dialog = new SampleDistributionEditWindow(null, _records) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"新增样品分发记录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not SampleDistributionRecord selected)
            {
                MessageBox.Show("请选择一条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SampleDistributionEditWindow(selected, _records) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"更新样品分发记录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not SampleDistributionRecord selected)
            {
                MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"确认删除样品分发记录 [{selected.ReceiveSendId}] 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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
                MessageBox.Show($"删除样品分发记录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            SampleSourceFilterCombo.Text = string.Empty;
            DateFromPicker.SelectedDate = null;
            DateToPicker.SelectedDate = null;
            OnlyReinspectionCheck.IsChecked = false;
            _recordView.Refresh();
        }

        private void ApplyButtonPermissions()
        {
            try
            {
                PagePermissionHelper.ApplyButtonPermissions(this, "sample_distribution", _currentRole, _db);
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
