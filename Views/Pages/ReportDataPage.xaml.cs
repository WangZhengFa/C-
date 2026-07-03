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
    /// 报告数据页面
    /// </summary>
    public partial class ReportDataPage : Page
    {
        public event EventHandler? CloseRequested;

        private readonly ReportDataService _service;
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly ObservableCollection<ReportDataRecord> _records = new();
        private readonly ICollectionView _recordView;

        public ReportDataPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public ReportDataPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            _service = new ReportDataService();

            _recordView = CollectionViewSource.GetDefaultView(_records);
            _recordView.Filter = RecordFilter;
            RecordGrid.ItemsSource = _recordView;

            InitFilterOptions();
            ApplyButtonPermissions();
            LoadRecords();
        }

        private void InitFilterOptions()
        {
            ConclusionFilterCombo.Items.Clear();
            ConclusionFilterCombo.Items.Add(string.Empty);
            ConclusionFilterCombo.Items.Add("合格");
            ConclusionFilterCombo.Items.Add("不合格");
            ConclusionFilterCombo.Items.Add("待定");
        }

        private void LoadRecords()
        {
            _records.Clear();
            foreach (var item in _service.ListAll())
            {
                _records.Add(item);
            }

            var current = ConclusionFilterCombo.Text?.Trim() ?? string.Empty;
            var values = _records.Select(x => x.Conclusion)
                                 .Where(x => !string.IsNullOrWhiteSpace(x))
                                 .Distinct()
                                 .OrderBy(x => x)
                                 .ToList();
            InitFilterOptions();
            foreach (var value in values)
            {
                if (!ConclusionFilterCombo.Items.Contains(value))
                {
                    ConclusionFilterCombo.Items.Add(value);
                }
            }
            ConclusionFilterCombo.Text = current;

            _recordView.Refresh();
        }

        private bool RecordFilter(object item)
        {
            if (item is not ReportDataRecord record)
            {
                return false;
            }

            var keyword = KeywordFilterText.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var hit = Contains(record.NodeCode, keyword)
                          || Contains(record.ReportNumber, keyword)
                          || Contains(record.SampleName, keyword)
                          || Contains(record.SampleBatch, keyword)
                          || Contains(record.TestingInstitution, keyword)
                          || Contains(record.Remark, keyword);
                if (!hit)
                {
                    return false;
                }
            }

            var conclusion = ConclusionFilterCombo.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(conclusion) && !string.Equals(record.Conclusion ?? string.Empty, conclusion, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var from = DateFromPicker.SelectedDate;
            if (from.HasValue && (!record.ReportDate.HasValue || record.ReportDate.Value.Date < from.Value.Date))
            {
                return false;
            }

            var to = DateToPicker.SelectedDate;
            if (to.HasValue && (!record.ReportDate.HasValue || record.ReportDate.Value.Date > to.Value.Date))
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
            var dialog = new ReportDataEditWindow(null, _records) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"新增报告数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not ReportDataRecord selected)
            {
                MessageBox.Show("请选择一条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new ReportDataEditWindow(selected, _records) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"更新报告数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not ReportDataRecord selected)
            {
                MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"确认删除报告数据 [{selected.ReportNumber}] 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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
                MessageBox.Show($"删除报告数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            ConclusionFilterCombo.Text = string.Empty;
            DateFromPicker.SelectedDate = null;
            DateToPicker.SelectedDate = null;
            _recordView.Refresh();
        }

        private void ApplyButtonPermissions()
        {
            try
            {
                PagePermissionHelper.ApplyButtonPermissions(this, "report_data", _currentRole, _db);
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
