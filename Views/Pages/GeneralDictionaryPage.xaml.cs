using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using MySqlConnector;
using FoodEnterpriseIMS.Helpers;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;
using FoodEnterpriseIMS.TreeCore;
using 食品信息管理系统.Views.Dialogs;
using DrawingFont = System.Drawing.Font;
using WF = System.Windows.Forms;

namespace 食品信息管理系统.Views.Pages
{
    public partial class GeneralDictionaryPage : Page
    {
        public event EventHandler? CloseRequested;

        private readonly CommonDictDataService _service;
        private readonly CommonDictFieldDefsService _fieldDefsService;
        private readonly DataGridColumnSettingsService _columnSettingsService;
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly ObservableCollection<CommonDictDataRecord> _records = new();
        private readonly ICollectionView _recordView;
        private readonly WF.TreeView _categoryTree = new();

        private string _selectedNodeCode = string.Empty;
        private bool _treeWidthSyncing;

        private const string AutoCodeConfigType = "CommonDictAutoCode";
        private const string GeneralDictExportDirKey = "general_dictionary.default_export_dir";

        public GeneralDictionaryPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public GeneralDictionaryPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            _service = new CommonDictDataService();
            _fieldDefsService = new CommonDictFieldDefsService();
            _columnSettingsService = new DataGridColumnSettingsService(_db, "ui");

            _recordView = CollectionViewSource.GetDefaultView(_records);
            _recordView.Filter = RecordFilter;
            RecordGrid.ItemsSource = _recordView;

            InitializeClassicTree();

            ApplyButtonPermissions();
            LoadRecords();
            UpdateAutoCodeControlsEnabled();
        }

        private void InitializeClassicTree()
        {
            _categoryTree.BorderStyle = WF.BorderStyle.None;
            _categoryTree.ShowLines = true;
            _categoryTree.ShowPlusMinus = true;
            _categoryTree.ShowRootLines = true;
            _categoryTree.HideSelection = false;
            _categoryTree.FullRowSelect = true;
            _categoryTree.HotTracking = false;
            _categoryTree.Indent = 18;
            _categoryTree.ItemHeight = 22;
            _categoryTree.Font = new DrawingFont("Microsoft YaHei", 9f);

            _categoryTree.AfterSelect += CategoryTree_AfterSelect;
            _categoryTree.AfterExpand += (_, _) => Dispatcher.BeginInvoke(SyncLocalTreeWidth, DispatcherPriority.Background);
            _categoryTree.AfterCollapse += (_, _) => Dispatcher.BeginInvoke(SyncLocalTreeWidth, DispatcherPriority.Background);

            CategoryTreeHost.Child = _categoryTree;
        }

        private static MySqlConnection CreateDbConnection()
        {
            var cfg = FoodEnterpriseIMS.Database.MysqlDbInitializer.LoadMysqlConfig();
            var conn = new MySqlConnection(FoodEnterpriseIMS.Database.MysqlDbInitializer.GetConnString(cfg));
            conn.Open();
            return conn;
        }

        private void LoadRecords()
        {
            _records.Clear();
            foreach (var item in _service.ListAll())
            {
                _records.Add(item);
            }

            BuildCategoryTree();
            RefreshFieldHeaders();
            _recordView.Refresh();
        }

        private void RefreshFieldHeaders()
        {
            if (RecordGrid.Columns.Count < 17)
            {
                return;
            }

            var labels = _fieldDefsService.ListAll()
                .Where(x => x.IsEnabled)
                .Where(x => string.Equals((x.NodeCode ?? string.Empty).Trim(), (_selectedNodeCode ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.SortOrder)
                .GroupBy(x => (x.FieldKey ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.First().FieldLabel) ? g.Key : g.First().FieldLabel.Trim(), StringComparer.OrdinalIgnoreCase);

            RecordGrid.Columns[3].Header = labels.TryGetValue("field1", out var field1) ? field1 : "字段1";
            RecordGrid.Columns[4].Header = labels.TryGetValue("field2", out var field2) ? field2 : "字段2";
            RecordGrid.Columns[5].Header = labels.TryGetValue("field3", out var field3) ? field3 : "字段3";
            RecordGrid.Columns[6].Header = labels.TryGetValue("field4", out var field4) ? field4 : "字段4";
            RecordGrid.Columns[7].Header = labels.TryGetValue("field5", out var field5) ? field5 : "字段5";
            RecordGrid.Columns[8].Header = labels.TryGetValue("number1", out var number1) ? number1 : "数值1";
            RecordGrid.Columns[9].Header = labels.TryGetValue("number2", out var number2) ? number2 : "数值2";
            RecordGrid.Columns[10].Header = labels.TryGetValue("date1", out var date1) ? date1 : "日期1";
            RecordGrid.Columns[11].Header = labels.TryGetValue("date2", out var date2) ? date2 : "日期2";
            RecordGrid.Columns[12].Header = labels.TryGetValue("flag1", out var flag1) ? flag1 : "标记1";
            RecordGrid.Columns[13].Header = labels.TryGetValue("flag2", out var flag2) ? flag2 : "标记2";
            RecordGrid.Columns[14].Header = labels.TryGetValue("amount", out var amount) ? amount : "金额";

            ApplyColumnVisibilitySettings();
            ApplySavedColumnOrder();
        }

        private static string GetColumnKey(DataGridColumn column)
        {
            if (column is DataGridBoundColumn bound && bound.Binding is Binding binding && binding.Path != null)
            {
                return binding.Path.Path ?? string.Empty;
            }

            return column.Header?.ToString() ?? string.Empty;
        }

        private string GetColumnConfigKey()
        {
            var node = string.IsNullOrWhiteSpace(_selectedNodeCode) ? "ALL" : _selectedNodeCode.Trim().ToUpperInvariant();
            return $"general_dictionary.columns.{node}";
        }

        private void SaveColumnVisibilitySetting(List<string> hiddenColumnKeys)
        {
            _columnSettingsService.Save(GetColumnConfigKey(), hiddenColumnKeys);
        }

        private void ApplyColumnVisibilitySettings()
        {
            _columnSettingsService.Apply(RecordGrid, GetColumnConfigKey(), GetColumnKey);
        }

        private void SaveColumnOrder(IEnumerable<string> orderedKeys)
        {
            _db.SetSystemConfig("ui", GetColumnConfigKey() + ".order", string.Join(";", orderedKeys));
        }

        private void ApplySavedColumnOrder()
        {
            var raw = _db.GetSystemConfig($"ui:{GetColumnConfigKey()}.order") ?? string.Empty;
            var order = raw.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
            if (order.Count == 0)
            {
                return;
            }

            var map = RecordGrid.Columns.ToDictionary(GetColumnKey, c => c, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < order.Count; i++)
            {
                if (map.TryGetValue(order[i], out var col))
                {
                    col.DisplayIndex = i;
                }
            }
        }

        private void BuildCategoryTree()
        {
            var currentCode = _selectedNodeCode;
            _categoryTree.Nodes.Clear();

            var loadedFromTreeNodes = false;
            try
            {
                using var conn = CreateDbConnection();
                var repo = new TreeRepository(conn, "common_dict");
                var ops = new TreeOperations(repo);
                var roots = ops.BuildTree();
                foreach (var root in roots.OrderBy(x => x.SortOrder))
                {
                    loadedFromTreeNodes = true;
                    _categoryTree.Nodes.Add(BuildTreeItem(root));
                }
            }
            catch
            {
                // ignore and fallback
            }

            if (!loadedFromTreeNodes)
            {
                var groups = _records.GroupBy(r => (r.NodeCode ?? string.Empty).Trim())
                    .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var group in groups)
                {
                    _categoryTree.Nodes.Add(new WF.TreeNode
                    {
                        Text = $"{group.Key} ({group.Count()})",
                        Tag = group.Key
                    });
                }
            }

            if (string.IsNullOrWhiteSpace(currentCode) || !SelectTreeNodeByCode(currentCode))
            {
                if (_categoryTree.Nodes.Count > 0)
                {
                    _categoryTree.SelectedNode = _categoryTree.Nodes[0];
                }
            }

            Dispatcher.BeginInvoke(SyncLocalTreeWidth, DispatcherPriority.Background);
        }

        private static WF.TreeNode BuildTreeItem(FoodEnterpriseIMS.TreeCore.TreeNode model)
        {
            var item = new WF.TreeNode
            {
                Text = model.Title,
                Tag = model.Code
            };

            foreach (var child in model.Children.OrderBy(x => x.SortOrder))
            {
                item.Nodes.Add(BuildTreeItem(child));
            }

            return item;
        }

        private bool SelectTreeNodeByCode(string code)
        {
            foreach (WF.TreeNode root in _categoryTree.Nodes)
            {
                if (TrySelectNodeRecursive(root, code))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TrySelectNodeRecursive(WF.TreeNode item, string code)
        {
            if (string.Equals(item.Tag?.ToString() ?? string.Empty, code, StringComparison.OrdinalIgnoreCase))
            {
                _categoryTree.SelectedNode = item;
                item.EnsureVisible();
                return true;
            }

            foreach (WF.TreeNode child in item.Nodes)
            {
                if (TrySelectNodeRecursive(child, code))
                {
                    item.Expand();
                    return true;
                }
            }

            return false;
        }

        private bool RecordFilter(object item)
        {
            if (item is not CommonDictDataRecord record)
            {
                return false;
            }

            var keyword = KeywordFilterText.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var hit = Contains(record.NodeCode, keyword)
                          || Contains(record.Code, keyword)
                          || Contains(record.Name, keyword)
                          || Contains(record.Field1, keyword)
                          || Contains(record.Field2, keyword)
                          || Contains(record.Field3, keyword)
                          || Contains(record.Field4, keyword)
                          || Contains(record.Field5, keyword)
                          || Contains(record.Remark, keyword);
                if (!hit)
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(_selectedNodeCode)
                && !string.Equals(record.NodeCode ?? string.Empty, _selectedNodeCode, StringComparison.OrdinalIgnoreCase))
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
            var dialog = new CommonDictDataEditWindow(null, _records) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"新增通用字典数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not CommonDictDataRecord selected)
            {
                MessageBox.Show("请选择一条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new CommonDictDataEditWindow(selected, _records) { Owner = Window.GetWindow(this) };
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
                MessageBox.Show($"更新通用字典数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not CommonDictDataRecord selected)
            {
                MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"确认删除字典数据 [{selected.Code}] 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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
                MessageBox.Show($"删除通用字典数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedRecord(-1);
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedRecord(1);
        }

        private void MoveSelectedRecord(int direction)
        {
            if (RecordGrid.SelectedItem is not CommonDictDataRecord selected)
            {
                MessageBox.Show("请选择一条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var currentList = _recordView.Cast<CommonDictDataRecord>().ToList();
            var index = currentList.FindIndex(r => r.Id == selected.Id);
            if (index < 0)
            {
                return;
            }

            var targetIndex = index + direction;
            if (targetIndex < 0 || targetIndex >= currentList.Count)
            {
                return;
            }

            var target = currentList[targetIndex];
            var temp = selected.SortOrder;
            selected.SortOrder = target.SortOrder;
            target.SortOrder = temp;

            try
            {
                _service.Update(selected);
                _service.Update(target);
                LoadRecords();
                SelectRecordById(selected.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"调整排序失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectRecordById(long id)
        {
            var target = _records.FirstOrDefault(r => r.Id == id);
            if (target == null)
            {
                return;
            }

            RecordGrid.SelectedItem = target;
            RecordGrid.ScrollIntoView(target);
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var configuredDir = GetPageSetting(GeneralDictExportDirKey);
            var dialog = new OpenFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
                Title = "导入通用字典数据"
            };

            if (!string.IsNullOrWhiteSpace(configuredDir) && Directory.Exists(configuredDir))
            {
                dialog.InitialDirectory = configuredDir;
            }

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var overwrite = MessageBox.Show(
                "是否清空导入文件中对应节点的原有数据后再导入？\n是：清空后导入；否：按 节点编码+编码 覆盖/新增。",
                "导入模式",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (overwrite == MessageBoxResult.Cancel)
            {
                return;
            }

            try
            {
                var result = _service.ImportFromCsv(dialog.FileName, overwrite == MessageBoxResult.Yes);
                MessageBox.Show($"导入完成：成功 {result.success} 条，失败 {result.fail} 条。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var configuredDir = GetPageSetting(GeneralDictExportDirKey);
            var dialog = new SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv",
                Title = "导出通用字典数据",
                FileName = $"通用字典_{DateTime.Now:yyyyMMddHHmmss}.csv"
            };

            if (!string.IsNullOrWhiteSpace(configuredDir) && Directory.Exists(configuredDir))
            {
                dialog.InitialDirectory = configuredDir;
            }

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _service.ExportToCsv(dialog.FileName, _records);
                SavePageSetting(GeneralDictExportDirKey, Path.GetDirectoryName(dialog.FileName) ?? string.Empty);
                MessageBox.Show("导出成功。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FieldDefsButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new CommonDictFieldDefsPage(_currentRole, _db);
            var host = new Window
            {
                Title = "定义字段",
                Width = 980,
                Height = 660,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                Content = page
            };

            page.CloseRequested += (_, _) => host.Close();
            host.ShowDialog();
            RefreshFieldHeaders();
        }

        private void ColumnSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var host = new Window
            {
                Title = "字段设置",
                Width = 760,
                Height = 620,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.CanMinimize
            };

            var root = new Grid { Margin = new Thickness(12) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var topButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 0, 8) };
            var previewButton = new Button { Content = "体验", Width = 72, Margin = new Thickness(0, 0, 8, 0) };
            var saveTopButton = new Button { Content = "保存", Width = 72, Margin = new Thickness(0, 0, 8, 0) };
            var closeTopButton = new Button { Content = "关闭", Width = 72 };
            topButtons.Children.Add(previewButton);
            topButtons.Children.Add(saveTopButton);
            topButtons.Children.Add(closeTopButton);
            Grid.SetRow(topButtons, 0);
            root.Children.Add(topButtons);

            var tabs = new TabControl();
            Grid.SetRow(tabs, 1);
            root.Children.Add(tabs);

            var tabOrder = new TabItem { Header = "显示与排序" };
            var tabDetail = new TabItem { Header = "字段详细设置" };
            var tabHeight = new TabItem { Header = "高度设置" };
            tabs.Items.Add(tabOrder);
            tabs.Items.Add(tabDetail);
            tabs.Items.Add(tabHeight);

            var orderGrid = new Grid { Margin = new Thickness(8) };
            orderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            orderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var orderList = new ListBox();
            var orderButtons = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
            Grid.SetColumn(orderList, 0);
            Grid.SetColumn(orderButtons, 1);
            orderGrid.Children.Add(orderList);
            orderGrid.Children.Add(orderButtons);
            tabOrder.Content = orderGrid;

            var btnToTop = new Button { Content = "⏮ 到顶", Width = 84, Margin = new Thickness(0, 0, 0, 8) };
            var btnUp = new Button { Content = "▲ 上移", Width = 84, Margin = new Thickness(0, 0, 0, 8) };
            var btnDown = new Button { Content = "▼ 下移", Width = 84, Margin = new Thickness(0, 0, 0, 8) };
            var btnToBottom = new Button { Content = "⏭ 到底", Width = 84 };
            orderButtons.Children.Add(btnToTop);
            orderButtons.Children.Add(btnUp);
            orderButtons.Children.Add(btnDown);
            orderButtons.Children.Add(btnToBottom);

            var detailGrid = new Grid { Margin = new Thickness(8) };
            detailGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            detailGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            detailGrid.Children.Add(new TextBlock { Text = "字段详细设置", Margin = new Thickness(0, 0, 0, 6) });
            var detailDataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = false,
                HeadersVisibility = DataGridHeadersVisibility.Column
            };
            detailDataGrid.Columns.Add(new DataGridTextColumn { Header = "字段", Binding = new Binding("Name"), IsReadOnly = true, Width = 160 });
            detailDataGrid.Columns.Add(new DataGridTextColumn { Header = "宽度", Binding = new Binding("Width"), Width = 90 });
            detailDataGrid.Columns.Add(new DataGridCheckBoxColumn { Header = "左端", Binding = new Binding("AlignLeft"), Width = 70 });
            detailDataGrid.Columns.Add(new DataGridCheckBoxColumn { Header = "居中", Binding = new Binding("AlignCenter"), Width = 70 });
            detailDataGrid.Columns.Add(new DataGridCheckBoxColumn { Header = "右端", Binding = new Binding("AlignRight"), Width = 70 });
            Grid.SetRow(detailDataGrid, 1);
            detailGrid.Children.Add(detailDataGrid);
            tabDetail.Content = detailGrid;

            var heightPanel = new StackPanel { Margin = new Thickness(12) };
            var rowHeightPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            rowHeightPanel.Children.Add(new TextBlock { Text = "行高", Width = 70, VerticalAlignment = VerticalAlignment.Center });
            var rowHeightText = new TextBox { Text = "30", Width = 80 };
            rowHeightPanel.Children.Add(rowHeightText);
            heightPanel.Children.Add(rowHeightPanel);

            var fontPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            fontPanel.Children.Add(new TextBlock { Text = "字体大小", Width = 70, VerticalAlignment = VerticalAlignment.Center });
            var fontSizeText = new TextBox { Text = "12", Width = 80 };
            fontPanel.Children.Add(fontSizeText);
            heightPanel.Children.Add(fontPanel);

            heightPanel.Children.Add(new TextBlock
            {
                Text = "仅新增/编辑窗体可用，主窗体已禁用以防误操作。",
                Foreground = Brushes.DimGray
            });
            tabHeight.Content = heightPanel;

            var checkMap = new Dictionary<string, CheckBox>(StringComparer.OrdinalIgnoreCase);
            var itemKeyMap = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);
            var detailRows = new ObservableCollection<ColumnSettingRow>();
            foreach (var column in RecordGrid.Columns.OrderBy(c => c.DisplayIndex))
            {
                var key = GetColumnKey(column);
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                var header = column.Header?.ToString() ?? key;
                var cb = new CheckBox
                {
                    IsChecked = column.Visibility == Visibility.Visible,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                };

                var text = new TextBlock
                {
                    Text = header,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.Black
                };

                var row = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(4, 3, 4, 3)
                };
                row.Children.Add(cb);
                row.Children.Add(text);

                orderList.Items.Add(row);
                checkMap[key] = cb;
                itemKeyMap[row] = key;
                detailRows.Add(new ColumnSettingRow { Name = header, Width = "150" });
            }
            detailDataGrid.ItemsSource = detailRows;

            btnUp.Click += (_, _) => MoveListItem(orderList, -1);
            btnDown.Click += (_, _) => MoveListItem(orderList, 1);
            btnToTop.Click += (_, _) => MoveListItemToEdge(orderList, true);
            btnToBottom.Click += (_, _) => MoveListItemToEdge(orderList, false);

            var bottomButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            var saveButton = new Button { Content = "保存", Width = 86, Margin = new Thickness(0, 0, 8, 0) };
            var cancelButton = new Button { Content = "取消", Width = 86 };
            bottomButtons.Children.Add(saveButton);
            bottomButtons.Children.Add(cancelButton);
            Grid.SetRow(bottomButtons, 2);
            root.Children.Add(bottomButtons);

            host.Content = root;

            previewButton.Click += (_, _) =>
            {
                SaveColumnOrder(GetOrderedKeys(orderList, itemKeyMap));
                ApplySavedColumnOrder();
            };
            saveTopButton.Click += (_, _) => saveButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            closeTopButton.Click += (_, _) => host.Close();

            saveButton.Click += (_, _) =>
            {
                var hiddenKeys = checkMap.Where(x => x.Value.IsChecked != true).Select(x => x.Key).ToList();
                SaveColumnVisibilitySetting(hiddenKeys);
                SaveColumnOrder(GetOrderedKeys(orderList, itemKeyMap));
                ApplyColumnVisibilitySettings();
                ApplySavedColumnOrder();

                if (double.TryParse(rowHeightText.Text?.Trim(), out var rowH) && rowH > 10)
                {
                    RecordGrid.RowHeight = rowH;
                }
                if (double.TryParse(fontSizeText.Text?.Trim(), out var fs) && fs > 6)
                {
                    RecordGrid.FontSize = fs;
                }

                host.DialogResult = true;
                host.Close();
            };
            cancelButton.Click += (_, _) => host.Close();

            host.ShowDialog();
        }

        private static List<string> GetOrderedKeys(ListBox orderList, Dictionary<object, string> itemKeyMap)
        {
            var keys = new List<string>();
            foreach (var item in orderList.Items)
            {
                if (item != null && itemKeyMap.TryGetValue(item, out var key) && !string.IsNullOrWhiteSpace(key))
                {
                    keys.Add(key);
                }
            }

            return keys;
        }

        private static void MoveListItem(ListBox list, int delta)
        {
            var idx = list.SelectedIndex;
            if (idx < 0)
            {
                return;
            }

            var target = idx + delta;
            if (target < 0 || target >= list.Items.Count)
            {
                return;
            }

            var item = list.Items[idx];
            list.Items.RemoveAt(idx);
            list.Items.Insert(target, item);
            list.SelectedIndex = target;
        }

        private static void MoveListItemToEdge(ListBox list, bool toTop)
        {
            var idx = list.SelectedIndex;
            if (idx < 0)
            {
                return;
            }

            var item = list.Items[idx];
            list.Items.RemoveAt(idx);
            var target = toTop ? 0 : list.Items.Count;
            list.Items.Insert(target, item);
            list.SelectedIndex = target;
        }

        private sealed class ColumnSettingRow
        {
            public string Name { get; set; } = string.Empty;
            public string Width { get; set; } = "150";
            public bool AlignLeft { get; set; }
            public bool AlignCenter { get; set; }
            public bool AlignRight { get; set; }
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new();

            public new bool Equals(object? x, object? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedNodeCode))
            {
                MessageBox.Show("请先选择左侧分类。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                SaveCurrentNodeData(_selectedNodeCode);
                MessageBox.Show("保存成功。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveCurrentNodeData(string nodeCode)
        {
            var rows = _records
                .Where(x => string.Equals((x.NodeCode ?? string.Empty).Trim(), nodeCode.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToList();

            if (rows.Count == 0)
            {
                return;
            }

            if (AutoCodeCheckBox.IsChecked == true)
            {
                EnsureAutoCodesForCurrentNode(rows);
                AssignSortOrderByCode(rows);
                SaveAutoCodeRuleForNode(nodeCode, silent: true);
            }

            EnsureNodeCodesUnique(rows);
            _service.UpdateMany(rows);
        }

        private void EnsureAutoCodesForCurrentNode(List<CommonDictDataRecord> list)
        {
            if (AutoCodeCheckBox.IsChecked != true || list.Count == 0)
            {
                return;
            }

            var used = new HashSet<string>(list
                .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                .Select(x => x.Code.Trim().ToUpperInvariant()));

            var mode = GetSelectedPrefixMode();
            var digits = GetDigits();
            var prefixLength = GetPrefixLength();
            var manual = GetManualPrefix();

            foreach (var item in list)
            {
                if (!string.IsNullOrWhiteSpace(item.Code) || string.IsNullOrWhiteSpace(item.Name))
                {
                    continue;
                }

                var prefix = mode == "manual" ? manual : GetInitialsByPinyin(item.Name, prefixLength);
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    continue;
                }

                var n = 1;
                string code;
                do
                {
                    code = digits > 0 ? $"{prefix}{n.ToString().PadLeft(digits, '0')}" : prefix;
                    n++;
                }
                while (used.Contains(code.ToUpperInvariant()));

                item.Code = code;
                used.Add(code.ToUpperInvariant());
            }
        }

        private static void AssignSortOrderByCode(List<CommonDictDataRecord> rows)
        {
            var ordered = rows
                .OrderBy(x => x.Code ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Id)
                .ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i].SortOrder = i + 1;
            }
        }

        private static void EnsureNodeCodesUnique(List<CommonDictDataRecord> rows)
        {
            var duplicates = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                .GroupBy(x => x.Code.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Count > 0)
            {
                throw new InvalidOperationException($"存在重复编码：{string.Join("、", duplicates)}");
            }
        }

        private static string GetInitialsByPinyin(string text, int length)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var ch in text.Trim())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(char.ToUpperInvariant(ch));
                }
                else
                {
                    var initial = GetChineseInitial(ch);
                    if (initial != '\0')
                    {
                        sb.Append(initial);
                    }
                }

                if (sb.Length >= length)
                {
                    break;
                }
            }

            var value = sb.ToString().ToUpperInvariant();
            if (value.Length == 0)
            {
                return string.Empty;
            }

            return value.Length < length ? value.PadRight(length, 'X') : value;
        }

        private static char GetChineseInitial(char ch)
        {
            try
            {
                var gb2312 = Encoding.GetEncoding("GB2312");
                var bytes = gb2312.GetBytes(ch.ToString());
                if (bytes.Length < 2) return '\0';
                var code = bytes[0] * 256 + bytes[1] - 65536;
                if (code >= -20319 && code <= -20284) return 'A';
                if (code >= -20283 && code <= -19776) return 'B';
                if (code >= -19775 && code <= -19219) return 'C';
                if (code >= -19218 && code <= -18711) return 'D';
                if (code >= -18710 && code <= -18527) return 'E';
                if (code >= -18526 && code <= -18240) return 'F';
                if (code >= -18239 && code <= -17923) return 'G';
                if (code >= -17922 && code <= -17418) return 'H';
                if (code >= -17417 && code <= -16475) return 'J';
                if (code >= -16474 && code <= -16213) return 'K';
                if (code >= -16212 && code <= -15641) return 'L';
                if (code >= -15640 && code <= -15166) return 'M';
                if (code >= -15165 && code <= -14923) return 'N';
                if (code >= -14922 && code <= -14915) return 'O';
                if (code >= -14914 && code <= -14631) return 'P';
                if (code >= -14630 && code <= -14150) return 'Q';
                if (code >= -14149 && code <= -14091) return 'R';
                if (code >= -14090 && code <= -13319) return 'S';
                if (code >= -13318 && code <= -12839) return 'T';
                if (code >= -12838 && code <= -12557) return 'W';
                if (code >= -12556 && code <= -11848) return 'X';
                if (code >= -11847 && code <= -11056) return 'Y';
                if (code >= -11055 && code <= -10247) return 'Z';
            }
            catch
            {
                return '\0';
            }

            return '\0';
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _recordView.Refresh();
        }

        private void KeywordFilterText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            _recordView.Refresh();
            e.Handled = true;
        }

        private void KeywordFilterText_TextChanged(object sender, TextChangedEventArgs e)
        {
            _recordView.Refresh();
        }

        private void CategoryTree_AfterSelect(object? sender, WF.TreeViewEventArgs e)
        {
            if (e.Node == null)
            {
                _selectedNodeCode = string.Empty;
                LoadAutoCodeRuleForNode(string.Empty);
                RefreshFieldHeaders();
                _recordView.Refresh();
                return;
            }

            _selectedNodeCode = e.Node.Tag?.ToString() ?? string.Empty;
            LoadAutoCodeRuleForNode(_selectedNodeCode);
            RefreshFieldHeaders();
            _recordView.Refresh();
        }

        private void SyncLocalTreeWidth()
        {
            if (_treeWidthSyncing)
            {
                return;
            }

            _treeWidthSyncing = true;
            try
            {
                ColCategoryTree.Width = new GridLength(CalcTreeContentWidth());
            }
            finally
            {
                _treeWidthSyncing = false;
            }
        }

        private double CalcTreeContentWidth()
        {
            var maxWidth = 120.0;
            const double baseLeftMargin = 16.0;
            const double indicatorPadding = 16.0;
            const double rowPadding = 8.0;
            var indentation = Math.Max(12.0, _categoryTree.Indent);

            foreach (WF.TreeNode root in _categoryTree.Nodes)
            {
                WalkTreeWidth(root, 1, ref maxWidth, baseLeftMargin, indicatorPadding, rowPadding, indentation);
            }

            return Math.Clamp(maxWidth + 8, 120, 420);
        }

        private void WalkTreeWidth(WF.TreeNode item, int depth, ref double maxWidth, double baseLeftMargin, double indicatorPadding, double rowPadding, double indentation)
        {
            var text = item.Text ?? string.Empty;
            var textWidth = MeasureTextWidth(text, _categoryTree.Font);
            var branchWidth = Math.Max(0, depth - 1) * indentation;
            var expandWidth = item.Nodes.Count > 0 ? indicatorPadding : 10;
            var total = baseLeftMargin + textWidth + branchWidth + expandWidth + rowPadding;
            if (total > maxWidth)
            {
                maxWidth = total;
            }

            if (!item.IsExpanded)
            {
                return;
            }

            foreach (WF.TreeNode child in item.Nodes)
            {
                WalkTreeWidth(child, depth + 1, ref maxWidth, baseLeftMargin, indicatorPadding, rowPadding, indentation);
            }
        }

        private static double MeasureTextWidth(string text, DrawingFont font)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            var size = WF.TextRenderer.MeasureText(text, font);
            return size.Width;
        }

        private void AutoCodeCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateAutoCodeControlsEnabled();
        }

        private void PrefixModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PrefixText == null || AutoCodeCheckBox == null) return;
            PrefixText.IsEnabled = AutoCodeCheckBox.IsChecked == true && GetSelectedPrefixMode() == "manual";
        }

        private void UpdateAutoCodeControlsEnabled()
        {
            if (AutoCodeCheckBox == null || PrefixModeCombo == null || PrefixLengthText == null || DigitsText == null || ReorderCheckBox == null || RecodeButton == null || SaveAutoCodeConfigButton == null || PrefixText == null)
            {
                return;
            }

            var enabled = AutoCodeCheckBox.IsChecked == true;
            PrefixModeCombo.IsEnabled = enabled;
            PrefixLengthText.IsEnabled = enabled;
            DigitsText.IsEnabled = enabled;
            ReorderCheckBox.IsEnabled = enabled;
            RecodeButton.IsEnabled = enabled;
            SaveAutoCodeConfigButton.IsEnabled = enabled;
            PrefixText.IsEnabled = enabled && GetSelectedPrefixMode() == "manual";
        }

        private string GetSelectedPrefixMode()
        {
            if (PrefixModeCombo?.SelectedItem is ComboBoxItem item)
            {
                var content = item.Content?.ToString() ?? string.Empty;
                return string.Equals(content, "手动", StringComparison.OrdinalIgnoreCase) ? "manual" : "initials";
            }

            var text = PrefixModeCombo?.Text?.Trim() ?? string.Empty;
            return string.Equals(text, "手动", StringComparison.OrdinalIgnoreCase) ? "manual" : "initials";
        }

        private int GetPrefixLength()
        {
            return int.TryParse(PrefixLengthText.Text?.Trim(), out var length) ? Math.Clamp(length, 1, 4) : 2;
        }

        private int GetDigits()
        {
            return int.TryParse(DigitsText.Text?.Trim(), out var digits) ? Math.Clamp(digits, 0, 6) : 3;
        }

        private string GetManualPrefix()
        {
            return new string((PrefixText.Text ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }

        private void RecodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedNodeCode))
            {
                MessageBox.Show("请先选择左侧分类。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var mode = GetSelectedPrefixMode();
            var digits = GetDigits();
            var prefixLength = GetPrefixLength();
            var manual = GetManualPrefix();

            var list = _recordView.Cast<CommonDictDataRecord>()
                .Where(x => string.Equals((x.NodeCode ?? string.Empty).Trim(), _selectedNodeCode, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToList();

            if (ReorderCheckBox.IsChecked == true)
            {
                list = list.OrderBy(x => x.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id).ToList();
            }

            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sort = 1;
            foreach (var item in list)
            {
                var basePrefix = mode == "manual" ? manual : GetInitialsByPinyin(item.Name, prefixLength);
                if (string.IsNullOrWhiteSpace(basePrefix)) basePrefix = "DF";

                var n = 1;
                string code;
                do
                {
                    code = digits > 0 ? $"{basePrefix}{n.ToString().PadLeft(digits, '0')}" : basePrefix;
                    n++;
                }
                while (used.Contains(code));

                used.Add(code);
                item.Code = code;
                item.SortOrder = sort++;
            }

            try
            {
                EnsureNodeCodesUnique(list);
                _service.UpdateMany(list);
                SaveAutoCodeRuleForNode(_selectedNodeCode, silent: true);
                LoadRecords();
                MessageBox.Show($"已重排并保存 {list.Count} 条数据。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重排编码失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAutoCodeConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedNodeCode))
            {
                MessageBox.Show("请先选择左侧分类。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveAutoCodeRuleForNode(_selectedNodeCode, silent: false);
        }

        private void SaveAutoCodeRuleForNode(string nodeCode, bool silent)
        {
            var key = $"code_{nodeCode.ToUpperInvariant()}";
            var enabled = AutoCodeCheckBox.IsChecked == true ? "1" : "0";
            var mode = GetSelectedPrefixMode();
            var pfx = GetPrefixLength();
            var digits = GetDigits();
            var pfxText = GetManualPrefix();
            var reorder = ReorderCheckBox.IsChecked == true ? "1" : "0";

            var value = $"enabled:{enabled};mode:{mode};pfx:{pfx};digits:{digits};pfx_text:{pfxText};reorder:{reorder}";
            try
            {
                _db.SetSystemConfig(AutoCodeConfigType, key, value);
                if (!silent)
                {
                    MessageBox.Show("自动编码配置已保存。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存自动编码配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAutoCodeRuleForNode(string nodeCode)
        {
            if (string.IsNullOrWhiteSpace(nodeCode))
            {
                ApplyDefaultAutoCodeRule();
                return;
            }

            try
            {
                var value = _db.GetSystemConfig($"{AutoCodeConfigType}:code_{nodeCode.ToUpperInvariant()}");
                if (string.IsNullOrWhiteSpace(value))
                {
                    ApplyDefaultAutoCodeRule();
                    return;
                }

                var map = value.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Split(':', 2, StringSplitOptions.RemoveEmptyEntries))
                    .Where(x => x.Length == 2)
                    .ToDictionary(x => x[0], x => x[1], StringComparer.OrdinalIgnoreCase);

                AutoCodeCheckBox.IsChecked = map.TryGetValue("enabled", out var enabled) && enabled == "1";
                var mode = map.TryGetValue("mode", out var modeValue) ? modeValue : "initials";
                PrefixModeCombo.SelectedIndex = string.Equals(mode, "manual", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                PrefixLengthText.Text = map.TryGetValue("pfx", out var pfx) ? pfx : "2";
                DigitsText.Text = map.TryGetValue("digits", out var digits) ? digits : "3";
                PrefixText.Text = map.TryGetValue("pfx_text", out var pfxText) ? pfxText : string.Empty;
                ReorderCheckBox.IsChecked = map.TryGetValue("reorder", out var reorder) && reorder == "1";
            }
            catch
            {
                ApplyDefaultAutoCodeRule();
            }

            UpdateAutoCodeControlsEnabled();
        }

        private void ApplyDefaultAutoCodeRule()
        {
            AutoCodeCheckBox.IsChecked = false;
            PrefixModeCombo.SelectedIndex = 0;
            PrefixLengthText.Text = "2";
            DigitsText.Text = "3";
            PrefixText.Text = string.Empty;
            ReorderCheckBox.IsChecked = false;
            UpdateAutoCodeControlsEnabled();
        }

        private string GetPageSetting(string key)
        {
            try
            {
                return _db.GetSystemConfig($"page:{key}") ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void SavePageSetting(string key, string value)
        {
            _db.SetSystemConfig("page", key, value ?? string.Empty);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ApplyButtonPermissions()
        {
            try
            {
                PagePermissionHelper.ApplyButtonPermissions(this, "general_dictionary", _currentRole, _db);
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
