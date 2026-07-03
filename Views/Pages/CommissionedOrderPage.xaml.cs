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
    /// 委托订单页面
    /// </summary>
    public partial class CommissionedOrderPage : Page
    {
        public event EventHandler? CloseRequested;

        private readonly CommissionedOrderService _service;
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly ObservableCollection<CommissionedOrderRecord> _records = new();
        private readonly ICollectionView _recordView;

        public CommissionedOrderPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public CommissionedOrderPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            _service = new CommissionedOrderService();

            _recordView = CollectionViewSource.GetDefaultView(_records);
            _recordView.Filter = RecordFilter;
            RecordGrid.ItemsSource = _recordView;

            InitFilterOptions();
            ApplyButtonPermissions();
            LoadRecords();
        }

        private void InitFilterOptions()
        {
            OrderStatusFilterCombo.Items.Clear();
            OrderStatusFilterCombo.Items.Add(string.Empty);
            OrderStatusFilterCombo.Items.Add("待处理");
            OrderStatusFilterCombo.Items.Add("处理中");
            OrderStatusFilterCombo.Items.Add("已完成");
            OrderStatusFilterCombo.Items.Add("已取消");
        }

        private void LoadRecords()
        {
            _records.Clear();
            foreach (var item in _service.ListAll())
            {
                _records.Add(item);
            }

            var current = OrderStatusFilterCombo.Text?.Trim() ?? string.Empty;
            var statuses = _records.Select(x => x.OrderStatus)
                                   .Where(x => !string.IsNullOrWhiteSpace(x))
                                   .Distinct()
                                   .OrderBy(x => x)
                                   .ToList();
            InitFilterOptions();
            foreach (var status in statuses)
            {
                if (!OrderStatusFilterCombo.Items.Contains(status))
                {
                    OrderStatusFilterCombo.Items.Add(status);
                }
            }
            OrderStatusFilterCombo.Text = current;

            _recordView.Refresh();
        }

        private bool RecordFilter(object item)
        {
            if (item is not CommissionedOrderRecord record)
            {
                return false;
            }

            var keyword = KeywordFilterText.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var hit = Contains(record.OrderId, keyword)
                          || Contains(record.CustomerName, keyword)
                          || Contains(record.ContactPerson, keyword)
                          || Contains(record.ContactPhone, keyword)
                          || Contains(record.ProductName, keyword)
                          || Contains(record.Remark, keyword);
                if (!hit)
                {
                    return false;
                }
            }

            var status = OrderStatusFilterCombo.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(record.OrderStatus ?? string.Empty, status, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var from = DateFromPicker.SelectedDate;
            if (from.HasValue && (!record.OrderDate.HasValue || record.OrderDate.Value.Date < from.Value.Date))
            {
                return false;
            }

            var to = DateToPicker.SelectedDate;
            if (to.HasValue && (!record.OrderDate.HasValue || record.OrderDate.Value.Date > to.Value.Date))
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
            var dialog = new CommissionedOrderEditWindow(null, _records) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"新增委托订单失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not CommissionedOrderRecord selected)
            {
                MessageBox.Show("请选择一条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new CommissionedOrderEditWindow(selected, _records) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"更新委托订单失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not CommissionedOrderRecord selected)
            {
                MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"确认删除委托订单 [{selected.OrderId}] 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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
                MessageBox.Show($"删除委托订单失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            OrderStatusFilterCombo.Text = string.Empty;
            DateFromPicker.SelectedDate = null;
            DateToPicker.SelectedDate = null;
            _recordView.Refresh();
        }

        private void ApplyButtonPermissions()
        {
            try
            {
                PagePermissionHelper.ApplyButtonPermissions(this, "commissioned_order", _currentRole, _db);
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
