using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.Win32;
using FoodEnterpriseIMS.Helpers;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;
using 食品信息管理系统.Views.Dialogs;

namespace 食品信息管理系统.Views.Pages
{
    /// <summary>
    /// 质量监督页面
    /// </summary>
    public partial class QualitySupervisionPage : Page
    {
        /// <summary>
        /// 关闭请求事件，由主窗口处理
        /// </summary>
        public event EventHandler? CloseRequested;

        private readonly QualitySupervisionService _service;
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly DispatcherTimer _autoRefreshTimer = new DispatcherTimer();
        private ObservableCollection<QualitySupervision> _records;
        private readonly ICollectionView _recordView;
        private readonly DataGridSettingsManager _columnSettingsManager;
        private const string AutoRefreshSecondsKey = "quality_supervision.auto_refresh_seconds";
        private const string DefaultExportDirectoryKey = "quality_supervision.default_export_dir";

        public QualitySupervisionPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public QualitySupervisionPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            _service = new QualitySupervisionService();
            _records = new ObservableCollection<QualitySupervision>();
            _recordView = CollectionViewSource.GetDefaultView(_records);
            _recordView.Filter = RecordFilter;
            RecordGrid.ItemsSource = _recordView;
            _columnSettingsManager = new DataGridSettingsManager(RecordGrid, "quality_supervision");
            ApplyButtonPermissions();
            InitAutoRefresh();
            LoadRecords();
            InitFilterOptions();
            _columnSettingsManager.LoadAndApply();
        }

        private void LoadRecords()
        {
            _records.Clear();
            foreach (var r in _service.ListAll())
            {
                _records.Add(r);
            }

            RefreshFilterOptions();
            _recordView.Refresh();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            OpenEditWindow(null);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is QualitySupervision record)
            {
                OpenEditWindow(record);
            }
            else
            {
                MessageBox.Show("请选择一条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not QualitySupervision record)
            {
                MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show("确定删除选中的质量监督记录？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                _service.Delete(record.SupervisionId);
                _records.Remove(record);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var configuredDir = GetPageSetting(DefaultExportDirectoryKey);
            var dialog = new OpenFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
                Title = "导入质量监督记录"
            };
            if (!string.IsNullOrWhiteSpace(configuredDir) && Directory.Exists(configuredDir))
            {
                dialog.InitialDirectory = configuredDir;
            }
            if (dialog.ShowDialog() != true) return;

            try
            {
                var (success, fail) = _service.ImportFromCsv(dialog.FileName);
                MessageBox.Show($"导入完成：成功 {success} 条，失败 {fail} 条", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var configuredDir = GetPageSetting(DefaultExportDirectoryKey);
            var dialog = new SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv",
                Title = "导出质量监督记录",
                FileName = $"质量监督_{DateTime.Now:yyyyMMddHHmmss}.csv"
            };
            if (!string.IsNullOrWhiteSpace(configuredDir) && Directory.Exists(configuredDir))
            {
                dialog.InitialDirectory = configuredDir;
            }
            if (dialog.ShowDialog() != true) return;

            try
            {
                _service.ExportToCsv(dialog.FileName, _records);
                MessageBox.Show("导出成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _columnSettingsManager.OpenSettingsDialog(Window.GetWindow(this));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OpenEditWindow(QualitySupervision? record)
        {
            var window = new QualitySupervisionEditWindow(record)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
            LoadRecords();
        }

        private void ApplyButtonPermissions()
        {
            try
            {
                PagePermissionHelper.ApplyButtonPermissions(this, "quality_supervision", _currentRole, _db);
            }
            catch
            {
                // 权限应用失败时不阻断页面基础功能
            }
        }

        public void RefreshPermissionState()
        {
            ApplyButtonPermissions();
        }

        private void InitAutoRefresh()
        {
            _autoRefreshTimer.Tick += (_, _) => LoadRecords();

            var secondsText = GetPageSetting(AutoRefreshSecondsKey);
            if (int.TryParse(secondsText, out var seconds) && seconds >= 0)
            {
                ApplyAutoRefresh(seconds);
            }
            else
            {
                ApplyAutoRefresh(0);
            }
        }

        private void ApplyAutoRefresh(int seconds)
        {
            _autoRefreshTimer.Stop();
            if (seconds <= 0)
            {
                return;
            }

            _autoRefreshTimer.Interval = TimeSpan.FromSeconds(seconds);
            _autoRefreshTimer.Start();
        }

        private string? GetPageSetting(string key)
        {
            return _db.GetSystemConfig($"page:{key}");
        }

        private void InitFilterOptions()
        {
            ProjectCategoryFilterCombo.Items.Clear();
            ProjectCategoryFilterCombo.Items.Add(string.Empty);
            ProjectCategoryFilterCombo.Items.Add("生产环境");
            ProjectCategoryFilterCombo.Items.Add("原辅料管理");
            ProjectCategoryFilterCombo.Items.Add("工艺流程");
            ProjectCategoryFilterCombo.Items.Add("成品检验");
            ProjectCategoryFilterCombo.Items.Add("标签标识");
            ProjectCategoryFilterCombo.Items.Add("仓储运输");
            ProjectCategoryFilterCombo.Items.Add("其他");

            RectificationResultFilterCombo.Items.Clear();
            RectificationResultFilterCombo.Items.Add(string.Empty);
            RectificationResultFilterCombo.Items.Add("合格");
            RectificationResultFilterCombo.Items.Add("不合格");
            RectificationResultFilterCombo.Items.Add("整改中");
        }

        private void RefreshFilterOptions()
        {
            var categoryCurrent = ProjectCategoryFilterCombo.Text?.Trim() ?? string.Empty;
            var resultCurrent = RectificationResultFilterCombo.Text?.Trim() ?? string.Empty;

            var categories = _records.Select(x => x.ProjectCategory)
                                     .Where(x => !string.IsNullOrWhiteSpace(x))
                                     .Distinct()
                                     .OrderBy(x => x)
                                     .ToList();
            var results = _records.Select(x => x.RectificationResult)
                                  .Where(x => !string.IsNullOrWhiteSpace(x))
                                  .Distinct()
                                  .OrderBy(x => x)
                                  .ToList();

            InitFilterOptions();
            foreach (var category in categories)
            {
                if (!ProjectCategoryFilterCombo.Items.Contains(category))
                {
                    ProjectCategoryFilterCombo.Items.Add(category);
                }
            }

            foreach (var result in results)
            {
                if (!RectificationResultFilterCombo.Items.Contains(result))
                {
                    RectificationResultFilterCombo.Items.Add(result);
                }
            }

            ProjectCategoryFilterCombo.Text = categoryCurrent;
            RectificationResultFilterCombo.Text = resultCurrent;
        }

        private bool RecordFilter(object item)
        {
            if (item is not QualitySupervision record)
            {
                return false;
            }

            var keyword = KeywordFilterText.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var hit = Contains(record.SupervisionId, keyword)
                          || Contains(record.ProjectName, keyword)
                          || Contains(record.BatchNumber, keyword)
                          || Contains(record.Supervisor, keyword)
                          || Contains(record.NonCompliance, keyword)
                          || Contains(record.RectificationActions, keyword)
                          || Contains(record.Remarks, keyword);
                if (!hit)
                {
                    return false;
                }
            }

            var category = ProjectCategoryFilterCombo.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(category) && !string.Equals(record.ProjectCategory ?? string.Empty, category, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var result = RectificationResultFilterCombo.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(result) && !string.Equals(record.RectificationResult ?? string.Empty, result, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var from = DiscoveryDateFromPicker.SelectedDate;
            if (from.HasValue && record.DiscoveryDate.Date < from.Value.Date)
            {
                return false;
            }

            var to = DiscoveryDateToPicker.SelectedDate;
            if (to.HasValue && record.DiscoveryDate.Date > to.Value.Date)
            {
                return false;
            }

            return true;
        }

        private static bool Contains(string? input, string keyword)
        {
            return (input ?? string.Empty).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void OnFilterChanged(object sender, RoutedEventArgs e)
        {
            _recordView.Refresh();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            KeywordFilterText.Text = string.Empty;
            ProjectCategoryFilterCombo.Text = string.Empty;
            RectificationResultFilterCombo.Text = string.Empty;
            DiscoveryDateFromPicker.SelectedDate = null;
            DiscoveryDateToPicker.SelectedDate = null;
            _recordView.Refresh();
        }
    }
}
