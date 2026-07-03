using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    /// 报告发放页面
    /// </summary>
    public partial class ReportIssuancePage : Page
    {
        public event EventHandler? CloseRequested;

        private readonly ReportDistributionService _service;
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly ObservableCollection<ReportDistributionRecord> _records = new();
        private readonly ICollectionView _recordView;

        public ReportIssuancePage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public ReportIssuancePage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            _service = new ReportDistributionService();

            _recordView = CollectionViewSource.GetDefaultView(_records);
            _recordView.Filter = RecordFilter;
            RecordGrid.ItemsSource = _recordView;

            InitFilterOptions();
            ApplyButtonPermissions();
            LoadRecords();
        }

        private void InitFilterOptions()
        {
            ReceivedFilterCombo.Items.Clear();
            ReceivedFilterCombo.Items.Add("全部");
            ReceivedFilterCombo.Items.Add("是");
            ReceivedFilterCombo.Items.Add("否");
            ReceivedFilterCombo.SelectedIndex = 0;

            AcceptedFilterCombo.Items.Clear();
            AcceptedFilterCombo.Items.Add("全部");
            AcceptedFilterCombo.Items.Add("是");
            AcceptedFilterCombo.Items.Add("否");
            AcceptedFilterCombo.SelectedIndex = 0;
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
            if (item is not ReportDistributionRecord record)
            {
                return false;
            }

            var keyword = KeywordFilterText.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var hit = Contains(record.ReportCode, keyword)
                          || Contains(record.Distributor, keyword)
                          || Contains(record.Recipient, keyword)
                          || Contains(record.Acceptor, keyword)
                          || Contains(record.Remarks, keyword);
                if (!hit)
                {
                    return false;
                }
            }

            if (!MatchesBoolFilter(record.IsReceived, ReceivedFilterCombo.SelectedItem?.ToString()))
            {
                return false;
            }

            if (!MatchesBoolFilter(record.IsAccepted, AcceptedFilterCombo.SelectedItem?.ToString()))
            {
                return false;
            }

            var from = DateFromPicker.SelectedDate;
            if (from.HasValue && (!record.DistributionDate.HasValue || record.DistributionDate.Value.Date < from.Value.Date))
            {
                return false;
            }

            var to = DateToPicker.SelectedDate;
            if (to.HasValue && (!record.DistributionDate.HasValue || record.DistributionDate.Value.Date > to.Value.Date))
            {
                return false;
            }

            return true;
        }

        private static bool MatchesBoolFilter(bool value, string? filter)
        {
            var text = filter ?? "全部";
            if (text == "全部") return true;
            if (text == "是") return value;
            if (text == "否") return !value;
            return true;
        }

        private static bool Contains(string? text, string keyword)
        {
            return (text ?? string.Empty).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ReportIssuanceEditWindow(null) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"新增报告发放失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not ReportDistributionRecord selected)
            {
                MessageBox.Show("请选择一条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new ReportIssuanceEditWindow(selected) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"更新报告发放失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not ReportDistributionRecord selected)
            {
                MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"确认删除报告发放记录 [{selected.ReportCode}] 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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
                MessageBox.Show($"删除报告发放失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            ReceivedFilterCombo.SelectedIndex = 0;
            AcceptedFilterCombo.SelectedIndex = 0;
            DateFromPicker.SelectedDate = null;
            DateToPicker.SelectedDate = null;
            _recordView.Refresh();
        }

        private void ApplyButtonPermissions()
        {
            try
            {
                PagePermissionHelper.ApplyButtonPermissions(this, "report_issuance", _currentRole, _db);
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
