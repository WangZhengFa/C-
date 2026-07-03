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
    /// 员工信息页面
    /// </summary>
    public partial class EmployeeInfoPage : Page
    {
        public event EventHandler? CloseRequested;

        private readonly EmployeeInfoService _service;
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly ObservableCollection<EmployeeInfo> _records = new();
        private readonly ICollectionView _recordView;

        public EmployeeInfoPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public EmployeeInfoPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            _service = new EmployeeInfoService();

            _recordView = CollectionViewSource.GetDefaultView(_records);
            _recordView.Filter = RecordFilter;
            RecordGrid.ItemsSource = _recordView;

            InitFilterOptions();
            ApplyButtonPermissions();
            LoadRecords();
        }

        private void InitFilterOptions()
        {
            DepartmentFilterCombo.Items.Clear();
            DepartmentFilterCombo.Items.Add(string.Empty);

            StatusFilterCombo.Items.Clear();
            StatusFilterCombo.Items.Add(string.Empty);
            StatusFilterCombo.Items.Add("在职");
            StatusFilterCombo.Items.Add("离职");
            StatusFilterCombo.Items.Add("停职");
        }

        private void LoadRecords()
        {
            _records.Clear();
            foreach (var item in _service.ListAll())
            {
                _records.Add(item);
            }

            var deptCurrent = DepartmentFilterCombo.Text?.Trim() ?? string.Empty;
            var departments = _records.Select(x => x.Department)
                                      .Where(x => !string.IsNullOrWhiteSpace(x))
                                      .Distinct()
                                      .OrderBy(x => x)
                                      .ToList();
            DepartmentFilterCombo.Items.Clear();
            DepartmentFilterCombo.Items.Add(string.Empty);
            foreach (var d in departments)
            {
                DepartmentFilterCombo.Items.Add(d);
            }
            DepartmentFilterCombo.Text = deptCurrent;

            _recordView.Refresh();
        }

        private bool RecordFilter(object item)
        {
            if (item is not EmployeeInfo record)
            {
                return false;
            }

            var keyword = KeywordFilterText.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var hit = Contains(record.EmployeeId, keyword)
                          || Contains(record.EmployeeName, keyword)
                          || Contains(record.Department, keyword)
                          || Contains(record.Title, keyword)
                          || Contains(record.Position, keyword)
                          || Contains(record.Phone, keyword)
                          || Contains(record.Email, keyword)
                          || Contains(record.Remark, keyword);
                if (!hit)
                {
                    return false;
                }
            }

            var dept = DepartmentFilterCombo.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(dept) && !string.Equals(record.Department ?? string.Empty, dept, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var status = StatusFilterCombo.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(record.Status ?? string.Empty, status, StringComparison.OrdinalIgnoreCase))
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
            var dialog = new EmployeeInfoEditWindow(null, _records) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"新增员工失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not EmployeeInfo selected)
            {
                MessageBox.Show("请选择一条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new EmployeeInfoEditWindow(selected, _records) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"更新员工失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not EmployeeInfo selected)
            {
                MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"确认删除员工 [{selected.EmployeeName}] 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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
                MessageBox.Show($"删除员工失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            DepartmentFilterCombo.Text = string.Empty;
            StatusFilterCombo.SelectedIndex = 0;
            _recordView.Refresh();
        }

        private void ApplyButtonPermissions()
        {
            try
            {
                PagePermissionHelper.ApplyButtonPermissions(this, "employee_info", _currentRole, _db);
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