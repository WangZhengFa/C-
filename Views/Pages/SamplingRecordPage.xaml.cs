using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.Win32;
using MySqlConnector;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Helpers;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;
using FoodEnterpriseIMS.TreeCore;
using 食品信息管理系统.Views.Dialogs;
using WF = System.Windows.Forms;

namespace 食品信息管理系统.Views.Pages
{
    /// <summary>
    /// 取样记录页面
    /// </summary>
    public partial class SamplingRecordPage : Page
    {
        /// <summary>
        /// 关闭请求事件，由主窗口处理
        /// </summary>
        public event EventHandler? CloseRequested;

        private readonly SamplingRecordService _service;
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly DispatcherTimer _autoRefreshTimer = new DispatcherTimer();
        private ObservableCollection<SamplingRecord> _records;
        private readonly ICollectionView _recordView;
        private readonly WF.TreeView _nodeTree = new();
        private const string AutoRefreshSecondsKey = "sample_record.auto_refresh_seconds";
        private const string DefaultExportDirectoryKey = "sample_record.default_export_dir";

        public SamplingRecordPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public SamplingRecordPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            _service = new SamplingRecordService();
            _records = new ObservableCollection<SamplingRecord>();
            _recordView = CollectionViewSource.GetDefaultView(_records);
            _recordView.Filter = RecordFilter;
            RecordGrid.ItemsSource = _recordView;
            InitializeNodeTree();
            ApplyButtonPermissions();
            InitAutoRefresh();
            LoadMaterialNodes();
            LoadRecords();
            InitFilterOptions();
        }

        private void InitializeNodeTree()
        {
            _nodeTree.BorderStyle = WF.BorderStyle.None;
            _nodeTree.ShowLines = true;
            _nodeTree.ShowPlusMinus = true;
            _nodeTree.ShowRootLines = true;
            _nodeTree.FullRowSelect = true;
            _nodeTree.HideSelection = false;
            _nodeTree.Indent = 18;
            _nodeTree.ItemHeight = 22;
            _nodeTree.Font = new Font("Microsoft YaHei", 9f);
            _nodeTree.AfterSelect += NodeTree_AfterSelect;
            NodeTreeHost.Child = _nodeTree;
        }

        #region 加载数据
        /// <summary>
        /// 加载 material_nodes 树，仅加载 depth <= 2
        /// </summary>
        private void LoadMaterialNodes()
        {
            _nodeTree.Nodes.Clear();
            try
            {
                var cfg = MysqlDbInitializer.LoadMysqlConfig();
                var connStr = $"server={cfg.Host};port={cfg.Port};user={cfg.User};password={cfg.Password};database={cfg.Database};charset=utf8mb4;Pooling=true;Max Pool Size=10;Min Pool Size=1";
                using var conn = new MySqlConnection(connStr);
                conn.Open();
                var repo = new TreeRepository(conn, "material_nodes");
                var nodes = repo.ListNodes(2);
                BuildTree(_nodeTree.Nodes, nodes, null);
                _nodeTree.ExpandAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载物料节点失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void BuildTree(WF.TreeNodeCollection parent, List<Dictionary<string, object>> nodes, string? parentCode)
        {
            foreach (var node in nodes.Where(n => (n.GetValueOrDefault("parent_code") as string ?? string.Empty) == (parentCode ?? string.Empty)))
            {
                var code = node.GetValueOrDefault("code") as string ?? string.Empty;
                var title = node.GetValueOrDefault("title") as string ?? code;
                var item = new WF.TreeNode { Text = title, Tag = code };
                BuildTree(item.Nodes, nodes, code);
                parent.Add(item);
            }
        }

        private void LoadRecords(string? nodeCode = null)
        {
            _records.Clear();
            foreach (var r in _service.ListByNodeCode(nodeCode))
            {
                _records.Add(r);
            }

            RefreshFilterOptions();
            _recordView.Refresh();
        }

        private void NodeTree_AfterSelect(object? sender, WF.TreeViewEventArgs e)
        {
            if (e.Node != null)
            {
                LoadRecords(e.Node.Tag?.ToString());
            }
        }
        #endregion

        #region 工具栏事件
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            OpenEditWindow(null);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is SamplingRecord record)
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
            if (RecordGrid.SelectedItem is not SamplingRecord record)
            {
                MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show("确定删除选中的取样记录？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                _service.Delete(record.Id);
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
                Title = "导入取样记录"
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
                Title = "导出取样记录",
                FileName = $"取样记录_{DateTime.Now:yyyyMMddHHmmss}.csv"
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
            var currentSeconds = GetPageSetting(AutoRefreshSecondsKey);
            var currentDirectory = GetPageSetting(DefaultExportDirectoryKey);

            var dialog = new Window
            {
                Title = "取样记录设置",
                Width = 460,
                Height = 230,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Owner = Window.GetWindow(this)
            };

            var grid = new Grid { Margin = new Thickness(12) };
            for (var i = 0; i < 4; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lblSeconds = new TextBlock { Text = "自动刷新秒数(0关闭):", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 6, 8, 6) };
            Grid.SetRow(lblSeconds, 0);
            Grid.SetColumn(lblSeconds, 0);
            grid.Children.Add(lblSeconds);

            var secondsCombo = new ComboBox
            {
                IsEditable = true,
                IsTextSearchEnabled = true,
                Margin = new Thickness(0, 6, 0, 6)
            };
            secondsCombo.Items.Add("0");
            secondsCombo.Items.Add("10");
            secondsCombo.Items.Add("30");
            secondsCombo.Items.Add("60");
            secondsCombo.Items.Add("120");
            secondsCombo.Items.Add("300");
            secondsCombo.Text = string.IsNullOrWhiteSpace(currentSeconds) ? "0" : currentSeconds;
            Grid.SetRow(secondsCombo, 0);
            Grid.SetColumn(secondsCombo, 1);
            grid.Children.Add(secondsCombo);

            var lblDir = new TextBlock { Text = "默认导出目录:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 6, 8, 6) };
            Grid.SetRow(lblDir, 1);
            Grid.SetColumn(lblDir, 0);
            grid.Children.Add(lblDir);

            var txtDir = new TextBox { Text = currentDirectory ?? string.Empty, Margin = new Thickness(0, 6, 0, 6) };
            Grid.SetRow(txtDir, 1);
            Grid.SetColumn(txtDir, 1);
            grid.Children.Add(txtDir);

            var hint = new TextBlock
            {
                Text = "提示：默认导出目录留空则使用系统默认目录。",
                Margin = new Thickness(0, 8, 0, 0),
                Opacity = 0.8
            };
            Grid.SetRow(hint, 2);
            Grid.SetColumn(hint, 0);
            Grid.SetColumnSpan(hint, 2);
            grid.Children.Add(hint);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnSave = new Button { Content = "保存", Width = 86, Margin = new Thickness(0, 10, 8, 0) };
            var btnCancel = new Button { Content = "取消", Width = 86, Margin = new Thickness(0, 10, 0, 0) };

            btnSave.Click += (_, _) =>
            {
                if (!int.TryParse(secondsCombo.Text.Trim(), out var seconds) || seconds < 0)
                {
                    MessageBox.Show("自动刷新秒数必须是大于等于 0 的整数。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dir = txtDir.Text.Trim();
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                {
                    MessageBox.Show("默认导出目录不存在，请检查后再保存。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _db.SetSystemConfig("page", AutoRefreshSecondsKey, seconds.ToString());
                _db.SetSystemConfig("page", DefaultExportDirectoryKey, dir);
                ApplyAutoRefresh(seconds);
                dialog.DialogResult = true;
                dialog.Close();
            };

            btnCancel.Click += (_, _) => dialog.Close();
            buttons.Children.Add(btnSave);
            buttons.Children.Add(btnCancel);
            Grid.SetRow(buttons, 5);
            Grid.SetColumn(buttons, 1);
            grid.Children.Add(buttons);

            dialog.Content = grid;
            var saved = dialog.ShowDialog();
            if (saved == true)
            {
                MessageBox.Show("设置已保存。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        private void ApplyButtonPermissions()
        {
            try
            {
                PagePermissionHelper.ApplyButtonPermissions(this, "sample_record", _currentRole, _db);
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

        private void OpenEditWindow(SamplingRecord? record)
        {
            var window = new SamplingRecordEditWindow(record)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
            LoadRecords();
        }

        private void InitAutoRefresh()
        {
            _autoRefreshTimer.Tick += (_, _) =>
            {
                var nodeCode = _nodeTree.SelectedNode?.Tag?.ToString();
                LoadRecords(nodeCode);
            };

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
            SampleSourceFilterCombo.Items.Clear();
            SampleSourceFilterCombo.Items.Add(string.Empty);
            SampleSourceFilterCombo.Items.Add("生产企业");
            SampleSourceFilterCombo.Items.Add("流通环节");
            SampleSourceFilterCombo.Items.Add("餐饮环节");
            SampleSourceFilterCombo.Items.Add("网络抽样");
            SampleSourceFilterCombo.Items.Add("其他");
        }

        private void RefreshFilterOptions()
        {
            var current = SampleSourceFilterCombo.Text?.Trim() ?? string.Empty;
            var sources = _records.Select(x => x.SampleSource)
                                  .Where(x => !string.IsNullOrWhiteSpace(x))
                                  .Distinct()
                                  .OrderBy(x => x)
                                  .ToList();

            SampleSourceFilterCombo.Items.Clear();
            SampleSourceFilterCombo.Items.Add(string.Empty);
            SampleSourceFilterCombo.Items.Add("生产企业");
            SampleSourceFilterCombo.Items.Add("流通环节");
            SampleSourceFilterCombo.Items.Add("餐饮环节");
            SampleSourceFilterCombo.Items.Add("网络抽样");
            SampleSourceFilterCombo.Items.Add("其他");
            foreach (var source in sources)
            {
                if (!SampleSourceFilterCombo.Items.Contains(source))
                {
                    SampleSourceFilterCombo.Items.Add(source);
                }
            }

            SampleSourceFilterCombo.Text = current;
        }

        private bool RecordFilter(object item)
        {
            if (item is not SamplingRecord record)
            {
                return false;
            }

            var keyword = KeywordFilterText.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var hit = Contains(record.SamplingId, keyword)
                          || Contains(record.NodeCode, keyword)
                          || Contains(record.SampleName, keyword)
                          || Contains(record.SampleBatch, keyword)
                          || Contains(record.BrandSeries, keyword)
                          || Contains(record.Sampler, keyword)
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

            var from = SamplingDateFromPicker.SelectedDate;
            if (from.HasValue && record.SamplingDate.Date < from.Value.Date)
            {
                return false;
            }

            var to = SamplingDateToPicker.SelectedDate;
            if (to.HasValue && record.SamplingDate.Date > to.Value.Date)
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
            SampleSourceFilterCombo.Text = string.Empty;
            SamplingDateFromPicker.SelectedDate = null;
            SamplingDateToPicker.SelectedDate = null;
            _recordView.Refresh();
        }
    }
}
