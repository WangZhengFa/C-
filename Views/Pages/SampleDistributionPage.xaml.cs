using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Helpers;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;
using FoodEnterpriseIMS.TreeCore;
using MySqlConnector;
using 食品信息管理系统.Views.Dialogs;
using WF = System.Windows.Forms;

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
        private readonly WF.TreeView _nodeTree = new();
        private readonly DataGridSettingsManager _columnSettingsManager;
        private string? _currentNodeCode;

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
            _columnSettingsManager = new DataGridSettingsManager(RecordGrid, "sample_distribution");

            InitializeNodeTree();
            InitFilterOptions();
            ApplyButtonPermissions();
            LoadMaterialNodes();
            LoadRecords();
            _columnSettingsManager.LoadAndApply();
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

        private static void BuildTree(WF.TreeNodeCollection parent, System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>> nodes, string? parentCode)
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
            _currentNodeCode = nodeCode;
            _records.Clear();
            foreach (var item in _service.ListByNodeCode(nodeCode))
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
            var dialog = new SampleDistributionEditWindow(null, _records) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _service.Insert(dialog.Value);
                LoadRecords(_currentNodeCode);
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
                if (dialog.IsNew)
                {
                    _service.Insert(dialog.Value);
                }
                else
                {
                    dialog.Value.Id = selected.Id;
                    _service.Update(dialog.Value);
                }
                LoadRecords(_currentNodeCode);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存样品分发记录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                LoadRecords(_currentNodeCode);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除样品分发记录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadMaterialNodes();
            LoadRecords(_currentNodeCode);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _columnSettingsManager.OpenSettingsDialog(Window.GetWindow(this));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region 筛选
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
                          || Contains(record.NodeCode, keyword)
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

        private void InitFilterOptions()
        {
            SampleSourceFilterCombo.Items.Clear();
            SampleSourceFilterCombo.Items.Add(string.Empty);
        }
        #endregion

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
