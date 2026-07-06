using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json;
using FoodEnterpriseIMS.Helpers;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;
using FoodEnterpriseIMS.Themes;
using FoodEnterpriseIMS.TreeCore;
using MySqlConnector;
using 食品信息管理系统.Views.Pages;
using 食品信息管理系统.Views.Dialogs;
using 食品信息管理系统.Services;
using Timer = System.Timers.Timer;
using WF = System.Windows.Forms;

namespace FoodEnterpriseIMS
{
    /// <summary>
    /// 主应用窗口
    /// </summary>
    public partial class MainAppWindow : Window
    {
        #region 字段
        private readonly Dictionary<string, object> _config;
        private readonly DatabaseManager _db;
        private readonly string _currentUser;
        private readonly long _currentUserId;
        private readonly int _currentRole;

        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");

        private static void WriteLog(string message)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFilePath, logEntry);
            }
            catch
            {
                // 忽略日志写入错误
            }
        }
        private bool _leftCollapsed;
        private const double DefaultTreePanelWidth = 170;
        private const double MinTreePanelWidth = 150;
        private const double MaxTreePanelWidth = 220;
        private double _expandedTreePanelWidth = DefaultTreePanelWidth;
        private List<string> _rolePermissionKeys;
        private Dictionary<string, string> _menuKeyMap = new Dictionary<string, string>();
        private string _currentPageKey;
        private int? _pendingTreeSelection;
        private Dictionary<string, UIElement> _pages = new Dictionary<string, UIElement>();
        private bool _treeContextMenuAttached;
        private readonly WF.TreeView _menuTree = new WF.TreeView();
        private const string MenuTreeKey = "system_menu";

        // 定时器
        private readonly Timer _reconnectTimer = new Timer(60000); // 1分钟检查一次数据库连接
        private readonly Timer _idleTimer = new Timer();
        private readonly Timer _dbStatusTimer = new Timer(30000);
        private readonly Timer _weatherRefreshTimer = new Timer(10 * 60 * 1000);
        private readonly DispatcherTimer _dateTimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        private DateTime _lastActivityTime;
        private bool _idleExitEnabled = false;
        private int _idleExitTimeoutMs = 30 * 60 * 1000; // 30分钟，仅在显式启用空闲退出时生效
        private bool _isAutoExitInProgress;
        private bool _isReloginInProgress;
        private DateTime? _lastWeatherUpdatedAt;
        private const string WeatherCitySettingKey = "weather_city";
        private const string WeatherRefreshMinutesSettingKey = "weather_refresh_minutes";
        #endregion

        #region 构造函数
        public MainAppWindow() : this(null)
        {
        }

        public MainAppWindow(Dictionary<string, object>? config)
        {
            InitializeComponent();
            _config = config ?? new Dictionary<string, object>();
            _db = new DatabaseManager(_config.ContainsKey("db_path") ? _config["db_path"].ToString() : "FoodEnterpriseIMS.db");
            _currentUser = _config.ContainsKey("user_name") ? _config["user_name"].ToString() : "未知用户";
            _currentUserId = _config.ContainsKey("user_id") ? Convert.ToInt64(_config["user_id"]) : 0;
            _currentRole = _config.ContainsKey("role_id") ? Convert.ToInt32(_config["role_id"]) : 0;

            // 初始化字体
            FontManager.RegisterFonts();

            InitializeMainMenuTree();
            
            // 应用主题（优先读取数据库配置）
            ApplyThemePreference();
            
            // 设置窗口基础属性
            UiHelper.SetWindowIcon(this);
            UiHelper.ApplySafeGeometry(this, new System.Windows.Size(1280, 800));
            RestoreInitialGeometry();
            
            // 初始化定时器
            InitTimers();
            
            // 初始化状态栏
            InitStatusBar();
            
            // 注册活动事件过滤器
            EventManager.RegisterClassHandler(typeof(UIElement), UIElement.MouseMoveEvent, new MouseEventHandler(OnUserActivity));
            EventManager.RegisterClassHandler(typeof(UIElement), UIElement.KeyDownEvent, new KeyEventHandler(OnUserActivity));
            
            // 初始化回车跳转过滤器
            DialogEnterFocusNavigator.Init(Application.Current);
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化定时器
        /// </summary>
        private void InitTimers()
        {
            // 数据库重连定时器
            _reconnectTimer.Elapsed += (s, e) => CheckDbConnection();
            _reconnectTimer.Start();

            // 空闲退出定时器
            _idleTimer.Interval = 1000; // 1秒检查一次
            _idleTimer.Elapsed += (s, e) => OnIdleTimeout();
            _idleExitEnabled = false;
            _lastActivityTime = DateTime.Now;

            // 数据库状态检查定时器
            _dbStatusTimer.Elapsed += (s, e) => CheckDbStatus();
            _dbStatusTimer.Start();

            // 天气刷新定时器
            _weatherRefreshTimer.Interval = GetWeatherRefreshIntervalMs();
            _weatherRefreshTimer.Elapsed += (s, e) => WeatherHelper.RefreshWeather();
            _weatherRefreshTimer.Start();
        }

        /// <summary>
        /// 初始化状态栏
        /// </summary>
        private void InitStatusBar()
        {
            // 设置版本信息
            var version = _db.GetLatestVersion() ?? "1.0.0";
            LblVersion.Content = $"版本号：{version}";
            
            // 设置用户信息
            LblUser.Content = $"用户：{_currentUser}";
            
            // 初始化时间显示
            UpdateDateTime();
            _dateTimeTimer.Tick += DateTimeTimer_Tick;
            _dateTimeTimer.Start();
            
            // 初始化数据库状态
            CheckDbStatus();

            // 初始化天气状态
            InitWeatherStatus();
        }

        /// <summary>
        /// 检查数据库连接状态
        /// </summary>
        private void CheckDbStatus()
        {
            try
            {
                bool connected = _db.CheckConnection();
                Dispatcher.Invoke(() =>
                {
                    LblDbStatus.Content = connected ? "DB: 已连接" : "DB: 已断开";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    LblDbStatus.Content = "DB: 异常";
                });
                WriteLog($"[CheckDbStatus] 检查数据库状态失败: {ex.Message}");
            }
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 窗口加载完成
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WriteLog($"[Window_Loaded] 窗口加载完成，开始刷新菜单树");
            try
            {
                // 延迟刷新菜单树并订阅右键事件
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    RefreshMenuTree();
                    AttachTreeContextMenuEvents();
                }), System.Windows.Threading.DispatcherPriority.Background);
                WriteLog($"[Window_Loaded] RefreshMenuTree 已调度");
            }
            catch (Exception ex)
            {
                WriteLog($"[Window_Loaded] 错误: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 窗口关闭前确认，30秒未确认自动退出
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isReloginInProgress)
            {
                return;
            }

            if (!_isAutoExitInProgress)
            {
                var confirmWindow = new ConfirmExitWindow { Owner = this };
                var result = confirmWindow.ShowDialog();
                if (result != true)
                {
                    e.Cancel = true;
                    return;
                }
            }

            WeatherHelper.WeatherUpdated -= OnWeatherUpdated;
            WeatherHelper.WeatherError -= OnWeatherError;
            _weatherRefreshTimer.Stop();
            _dateTimeTimer.Stop();

            try
            {
                SaveCurrentGeometryConfig();

                // 关闭事件中避免同步阻塞 UI，防止退出流程被卡住。
                _ = AuthService.MarkLogoutAsync(_currentUserId);
            }
            catch (Exception ex)
            {
                WriteLog($"[Window_Closing] 更新离线状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 切换树面板显示/隐藏
        /// </summary>
        private void ToggleTreePanel_Click(object sender, MouseButtonEventArgs e)
        {
            _leftCollapsed = !_leftCollapsed;
            if (_leftCollapsed)
            {
                if (ColTreePanel.ActualWidth > 0)
                {
                    _expandedTreePanelWidth = ClampTreePanelWidth(ColTreePanel.ActualWidth);
                }

                ColTreePanel.Width = new GridLength(0);
            }
            else
            {
                ColTreePanel.Width = new GridLength(ClampTreePanelWidth(_expandedTreePanelWidth));
            }

            LblToggleIcon.Content = _leftCollapsed ? ">>" : "<<";
        }

        private void InitializeMainMenuTree()
        {
            ClassicWinFormsTreeHelper.ApplyClassicStyle(_menuTree, 18, 22);
            _menuTree.Font = new Font("Microsoft YaHei", 9f);

            _menuTree.AfterSelect += (_, e) =>
            {
                var pageKey = e.Node?.Tag?.ToString();
                if (!string.IsNullOrWhiteSpace(pageKey))
                {
                    OpenPageByKey(pageKey);
                }
            };

            _menuTree.NodeMouseDoubleClick += (_, e) =>
            {
                var pageKey = e.Node?.Tag?.ToString();
                if (!string.IsNullOrWhiteSpace(pageKey))
                {
                    OpenPageByKey(pageKey);
                }
            };

            _menuTree.NodeMouseClick += (_, e) =>
            {
                if (e.Button == WF.MouseButtons.Right)
                {
                    _menuTree.SelectedNode = e.Node;
                }
            };

            TreeMenuHost.Child = _menuTree;
        }

        /// <summary>
        /// 用户活动事件（重置空闲计时器）
        /// </summary>
        private void OnUserActivity(object sender, EventArgs e)
        {
            _lastActivityTime = DateTime.Now;
        }

        private void DateTimeTimer_Tick(object? sender, EventArgs e)
        {
            if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
            {
                return;
            }

            UpdateDateTime();
        }

        /// <summary>
        /// 更新日期时间显示
        /// </summary>
        private void UpdateDateTime()
        {
            LblDateTime.Content = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        #endregion

        #region 树右键菜单事件
        /// <summary>
        /// 订阅树控件右键菜单路由事件（幂等）
        /// </summary>
        private void AttachTreeContextMenuEvents()
        {
            if (_treeContextMenuAttached) return;
            _treeContextMenuAttached = true;

            ClassicWinFormsTreeHelper.AttachStandardContextMenu(_menuTree, new StandardTreeMenuActions
            {
                AddSibling = TreeMenu_NodeAddSiblingRequested,
                AddChild = TreeMenu_NodeAddChildRequested,
                EditNode = TreeMenu_NodeEditRequested,
                DeleteNode = TreeMenu_NodeDeleteRequested,

                CopySubtreeToRoot = TreeMenu_CopySubtreeToRoot,
                MoveNodeToTarget = TreeMenu_MoveNodeToTarget,
                MoveUp = TreeMenu_MoveUp,
                MoveDown = TreeMenu_MoveDown,
                MoveTop = TreeMenu_MoveTop,
                MoveBottom = TreeMenu_MoveBottom,

                AuditIntegrity = TreeMenu_AuditIntegrity,
                NormalizeAllSiblingSort = TreeMenu_NormalizeAllSort,
                ExportJson = TreeMenu_ExportJson,
                ImportJson = TreeMenu_ImportJson,

                ExpandCurrent = TreeMenu_NodeExpandRequested,
                CollapseCurrent = TreeMenu_NodeCollapseRequested,
                ExpandAll = () => _menuTree.ExpandAll(),
                CollapseAll = () => _menuTree.CollapseAll(),

                RefreshTree = RefreshMenuTree
            });
        }

        /// <summary>
        /// 新增节点
        /// </summary>
        private void TreeMenu_NodeAddChildRequested()
        {
            var parentNode = CreateModelNodeFromUi(_menuTree.SelectedNode);
            var newNode = new TreeNode
            {
                ParentCode = parentNode?.Code,
                SortOrder = 0
            };
            OpenNodeEditWindow(newNode, MenuTreeKey, isNew: true);
        }

        private void TreeMenu_NodeAddSiblingRequested()
        {
            var current = _menuTree.SelectedNode;
            var parentCode = current?.Parent?.Tag?.ToString();
            var newNode = new TreeNode
            {
                ParentCode = parentCode,
                SortOrder = 0
            };

            OpenNodeEditWindow(newNode, MenuTreeKey, isNew: true);
        }

        /// <summary>
        /// 编辑节点
        /// </summary>
        private void TreeMenu_NodeEditRequested()
        {
            var node = CreateModelNodeFromUi(_menuTree.SelectedNode);
            if (node == null || string.IsNullOrWhiteSpace(node.Code)) return;
            OpenNodeEditWindow(node, MenuTreeKey, isNew: false);
        }

        /// <summary>
        /// 删除节点
        /// </summary>
        private void TreeMenu_NodeDeleteRequested()
        {
            var node = CreateModelNodeFromUi(_menuTree.SelectedNode);
            if (node == null || string.IsNullOrWhiteSpace(node.Code)) return;

            var result = MessageBox.Show($"确定删除节点 [{node.Title}] 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using var conn = CreateDbConnection();
                var repo = new TreeRepository(conn, MenuTreeKey);
                repo.DeleteNode(node.Code);
                RefreshMenuTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 展开节点
        /// </summary>
        private void TreeMenu_NodeExpandRequested()
        {
            if (_menuTree.SelectedNode != null)
            {
                _menuTree.SelectedNode.Expand();
            }
        }

        /// <summary>
        /// 折叠节点
        /// </summary>
        private void TreeMenu_NodeCollapseRequested()
        {
            if (_menuTree.SelectedNode != null)
            {
                _menuTree.SelectedNode.Collapse();
            }
        }

        private void TreeMenu_CopySubtreeToRoot()
        {
            var node = CreateModelNodeFromUi(_menuTree.SelectedNode);
            if (node == null || string.IsNullOrWhiteSpace(node.Code))
            {
                return;
            }

            try
            {
                using var conn = CreateDbConnection();
                var repo = new TreeRepository(conn, MenuTreeKey);
                var ops = new TreeOperations(repo);
                var source = ops.FindNode(node.Code);
                if (source == null)
                {
                    MessageBox.Show("未找到待复制节点。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                CopyNodeRecursive(ops, source, null);
                RefreshMenuTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void CopyNodeRecursive(TreeOperations ops, TreeNode source, string? parentCode)
        {
            var copied = ops.AddNode(source.Title, parentCode, new Dictionary<string, object>(source.Payload));
            foreach (var child in source.Children.OrderBy(c => c.SortOrder))
            {
                CopyNodeRecursive(ops, child, copied.Code);
            }
        }

        private void TreeMenu_MoveNodeToTarget()
        {
            var source = _menuTree.SelectedNode;
            if (source == null)
            {
                MessageBox.Show("请先选择要移动的节点。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using var conn = CreateDbConnection();
                var repo = new TreeRepository(conn, MenuTreeKey);
                var ops = new TreeOperations(repo);

                var sourceCode = source.Tag?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(sourceCode))
                {
                    MessageBox.Show("当前节点缺少编码，无法移动。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var sourceNode = ops.FindNode(sourceCode);
                if (sourceNode == null)
                {
                    MessageBox.Show("未找到当前节点的完整数据。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new TreeNodeMoveWindow(ops.BuildTree(), sourceNode)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                ops.MoveNode(
                    sourceNode.Code,
                    dialog.SelectedParentCode,
                    regenerateCodes: dialog.RegenerateCodes,
                    targetIndex: dialog.TargetIndex);

                RefreshMenuTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移动失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TreeMenu_MoveUp()
        {
            MoveNodeInSameLevel(-1);
        }

        private void TreeMenu_MoveDown()
        {
            MoveNodeInSameLevel(1);
        }

        private void TreeMenu_MoveTop()
        {
            MoveNodeToEdge(true);
        }

        private void TreeMenu_MoveBottom()
        {
            MoveNodeToEdge(false);
        }

        private void MoveNodeInSameLevel(int delta)
        {
            var node = _menuTree.SelectedNode;
            if (node == null)
            {
                return;
            }

            var siblings = node.Parent?.Nodes ?? _menuTree.Nodes;
            var idx = node.Index;
            var target = idx + delta;
            if (target < 0 || target >= siblings.Count)
            {
                return;
            }

            var codes = siblings.Cast<WF.TreeNode>().Select(n => n.Tag?.ToString() ?? string.Empty).ToList();
            (codes[idx], codes[target]) = (codes[target], codes[idx]);

            try
            {
                using var conn = CreateDbConnection();
                var repo = new TreeRepository(conn, MenuTreeKey);
                var parentCode = node.Parent?.Tag?.ToString();
                repo.Resequence(parentCode, codes);
                RefreshMenuTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移动失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MoveNodeToEdge(bool toTop)
        {
            var node = _menuTree.SelectedNode;
            if (node == null)
            {
                return;
            }

            var siblings = node.Parent?.Nodes ?? _menuTree.Nodes;
            var codes = siblings.Cast<WF.TreeNode>().Select(n => n.Tag?.ToString() ?? string.Empty).ToList();
            codes.RemoveAt(node.Index);
            if (toTop)
            {
                codes.Insert(0, node.Tag?.ToString() ?? string.Empty);
            }
            else
            {
                codes.Add(node.Tag?.ToString() ?? string.Empty);
            }

            try
            {
                using var conn = CreateDbConnection();
                var repo = new TreeRepository(conn, MenuTreeKey);
                var parentCode = node.Parent?.Tag?.ToString();
                repo.Resequence(parentCode, codes);
                RefreshMenuTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移动失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TreeMenu_AuditIntegrity()
        {
            try
            {
                using var conn = CreateDbConnection();
                var repo = new TreeRepository(conn, MenuTreeKey);
                var ops = new TreeOperations(repo);
                var audit = ops.AuditIntegrity();
                var msg = $"无效编码: {audit["invalid_codes"].Count}\n缺失父节点: {audit["missing_parents"].Count}\n重复编码: {audit["duplicates"].Count}";
                MessageBox.Show(msg, "审计结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"审计失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TreeMenu_NormalizeAllSort()
        {
            try
            {
                using var conn = CreateDbConnection();
                var repo = new TreeRepository(conn, MenuTreeKey);
                var ops = new TreeOperations(repo);
                var groups = ops.NormalizeAllSortOrders();
                RefreshMenuTree();
                MessageBox.Show($"已规范 {groups} 组同级排序。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"规范排序失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TreeMenu_ExportJson()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON 文件 (*.json)|*.json",
                FileName = $"menu_tree_{DateTime.Now:yyyyMMddHHmmss}.json"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                using var conn = CreateDbConnection();
                var repo = new TreeRepository(conn, MenuTreeKey);
                var ops = new TreeOperations(repo);
                var data = ops.ExportTree();
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(dialog.FileName, json);
                MessageBox.Show("导出成功。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TreeMenu_ImportJson()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON 文件 (*.json)|*.json"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            if (MessageBox.Show("导入会覆盖现有树结构，是否继续？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(dialog.FileName);
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json) ?? new List<Dictionary<string, object>>();
                using var conn = CreateDbConnection();
                var repo = new TreeRepository(conn, MenuTreeKey);
                var ops = new TreeOperations(repo);
                ops.ImportTree(data, clearExisting: true);
                RefreshMenuTree();
                MessageBox.Show("导入成功。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private TreeNode? CreateModelNodeFromUi(WF.TreeNode? node)
        {
            if (node == null)
            {
                return null;
            }

            return new TreeNode
            {
                Code = node.Tag?.ToString() ?? string.Empty,
                Title = node.Text ?? string.Empty,
                ParentCode = node.Parent?.Tag?.ToString(),
                SortOrder = node.Index + 1
            };
        }

        /// <summary>
        /// 打开节点编辑窗口
        /// </summary>
        private void OpenNodeEditWindow(TreeNode node, string treeKey, bool isNew)
        {
            try
            {
                var dialogNode = node;
                if (!isNew)
                {
                    dialogNode = LoadFullNode(node.Code, treeKey) ?? node;
                }

                var dialog = new TreeNodeEditWindow(dialogNode, treeKey, isNew) { Owner = this };
                dialog.ShowDialog();
                if (dialog.Saved)
                {
                    RefreshMenuTree();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开编辑窗口失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 从数据库加载完整节点
        /// </summary>
        private TreeNode? LoadFullNode(string code, string treeKey)
        {
            using var conn = CreateDbConnection();
            var repo = new TreeRepository(conn, treeKey);
            var dict = repo.GetNode(code);
            if (dict == null) return null;

            return new TreeNode
            {
                Code = dict["code"]?.ToString() ?? "",
                Title = dict["title"]?.ToString() ?? "",
                ParentCode = dict["parent_code"]?.ToString(),
                SortOrder = Convert.ToInt32(dict["sort_order"] ?? 0),
                Payload = dict["payload"] is Dictionary<string, object> pl ? pl : new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// 创建数据库连接
        /// </summary>
        private MySqlConnection CreateDbConnection()
        {
            var cfg = FoodEnterpriseIMS.Database.MysqlDbInitializer.LoadMysqlConfig();
            var conn = new MySqlConnection(FoodEnterpriseIMS.Database.MysqlDbInitializer.GetConnString(cfg));
            conn.Open();
            return conn;
        }
        #endregion

        #region 核心业务逻辑
        /// <summary>
        /// 根据Key打开页面
        /// </summary>
        private void OpenPageByKey(string pageKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pageKey)) return;

                if (new[] { "relogin", "exit_system", "change_password", "about", "theme_settings", "tree_style_settings" }.Contains(pageKey))
                {
                    HandleSpecialActions(pageKey);
                    return;
                }

                RefreshRolePermissionsCache();
                if (_menuKeyMap.Count == 0)
                {
                    BuildMenuKeyMap();
                }

                var menuKey = _menuKeyMap.ContainsKey(pageKey) ? _menuKeyMap[pageKey] : pageKey;
                if (!IsMenuKeyAllowed(menuKey))
                {
                    MessageBox.Show("当前角色无此权限", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var menuItem = _db.GetMenuByKey(pageKey);
                if (menuItem == null) return;

                var title = menuItem.ContainsKey("title") ? menuItem["title"].ToString() : pageKey;
                var compPath = menuItem.ContainsKey("csharp_component_path")
                    ? menuItem["csharp_component_path"].ToString()
                    : "";
                var csharpClass = menuItem.ContainsKey("csharp_class") ? menuItem["csharp_class"].ToString() : "";
                if (!string.IsNullOrWhiteSpace(csharpClass) || !string.IsNullOrWhiteSpace(compPath))
                {
                    ShowContentPage(pageKey, title, csharpClass, compPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打开页面失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 显示内容页面
        /// </summary>
        private void ShowContentPage(string pageKey, string title, string? csharpClass = null, string? componentPath = null)
        {
            UIElement? page = null;
            var shouldCache = true;

            if (_pages.TryGetValue(pageKey, out var cachedPage)
                && cachedPage is TextBlock cachedText
                && (cachedText.Text?.StartsWith("未找到页面：", StringComparison.Ordinal) ?? false))
            {
                _pages.Remove(pageKey);
            }

            if (_pages.TryGetValue(pageKey, out cachedPage))
            {
                page = cachedPage;
            }

            if (page == null)
            {
                if (!string.IsNullOrWhiteSpace(csharpClass))
                {
                    page = CreatePageByReflection(csharpClass, title);
                    AttachCloseEvent(page);
                    AttachPermissionChangedEvent(page);
                }
                else if (!string.IsNullOrWhiteSpace(componentPath))
                {
                    var className = componentPath.Contains(".")
                        ? componentPath
                        : $"食品信息管理系统.Views.Pages.{componentPath}";
                    page = CreatePageByReflection(className, title);
                    AttachCloseEvent(page);
                    AttachPermissionChangedEvent(page);
                }
                else
                {
                    switch (pageKey)
                    {
                        case "sample_record":
                            var samplingPage = new SamplingRecordPage(_currentRole, _db);
                            samplingPage.CloseRequested += (s, e) => CloseContentPage();
                            page = samplingPage;
                            break;
                        case "quality_supervision":
                            var supervisionPage = new QualitySupervisionPage(_currentRole, _db);
                            supervisionPage.CloseRequested += (s, e) => CloseContentPage();
                            page = supervisionPage;
                            break;
                        default:
                            page = new TextBlock { Text = $"页面：{title}", FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                            break;
                    }
                }
            }

            if (page is Page)
            {
                // Page 必须挂在 Frame 或 Window 下，且不复用实例避免父级冲突。
                shouldCache = false;
            }
            else if (shouldCache)
            {
                _pages[pageKey] = page;
            }

            _currentPageKey = pageKey;

            LblPageTitle.Content = title;

            // 切换内容
            object tabContent = page;
            if (page is Page wpfPage)
            {
                var frame = new Frame
                {
                    NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden,
                    Content = wpfPage
                };
                tabContent = frame;
            }

            ContentArea.Content = tabContent;
        }

        /// <summary>
        /// 通过反射创建页面实例
        /// </summary>
        private UIElement CreatePageByReflection(string csharpClass, string title)
        {
            try
            {
                var type = Type.GetType(csharpClass)
                    ?? Type.GetType($"{csharpClass}, 食品信息管理系统")
                    ?? Type.GetType($"FoodEnterpriseIMS.{csharpClass}, 食品信息管理系统");

                if (type != null && typeof(UIElement).IsAssignableFrom(type))
                {
                    object? instance = null;
                    var roleCtor = type.GetConstructor(new[] { typeof(int), typeof(DatabaseManager) });
                    if (roleCtor != null)
                    {
                        instance = roleCtor.Invoke(new object[] { _currentRole, _db });
                    }
                    else
                    {
                        instance = Activator.CreateInstance(type);
                    }

                    if (instance is UIElement uiElement)
                    {
                        return uiElement;
                    }
                }
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                WriteLog($"[CreatePageByReflection] 反射创建页面失败 {csharpClass}: {detail}");
                MessageBox.Show($"页面加载失败：{title}\n{detail}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return new TextBlock
            {
                Text = $"未找到页面：{title} ({csharpClass})",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        /// <summary>
        /// 为反射创建的页面附加关闭事件
        /// </summary>
        private void AttachCloseEvent(UIElement page)
        {
            try
            {
                var closeEvent = page.GetType().GetEvent("CloseRequested");
                if (closeEvent != null && closeEvent.EventHandlerType == typeof(EventHandler))
                {
                    var handler = new EventHandler((s, e) => CloseContentPage());
                    closeEvent.AddEventHandler(page, handler);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"[AttachCloseEvent] 附加关闭事件失败: {ex.Message}");
            }
        }

        private void AttachPermissionChangedEvent(UIElement page)
        {
            try
            {
                var permissionEvent = page.GetType().GetEvent("PermissionsChanged");
                if (permissionEvent != null && permissionEvent.EventHandlerType == typeof(EventHandler))
                {
                    var handler = new EventHandler((s, e) => OnPermissionsChanged());
                    permissionEvent.AddEventHandler(page, handler);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"[AttachPermissionChangedEvent] 附加权限变更事件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 关闭当前内容页
        /// </summary>
        private void CloseContentPage()
        {
            ContentArea.Content = null;
            LblPageTitle.Content = "";
            _currentPageKey = string.Empty;
        }

        /// <summary>
        /// 处理特殊操作（退出、改密码等）
        /// </summary>
        private void HandleSpecialActions(string actionKey)
        {
            switch (actionKey)
            {
                case "exit_system":
                    Close();
                    break;
                case "about":
                    ShowAboutDialog();
                    break;
                case "change_password":
                    ShowChangePasswordDialog();
                    break;
                case "relogin":
                    Relogin();
                    break;
                case "theme_settings":
                    ShowThemeSettingsDialog();
                    break;
                case "tree_style_settings":
                    ShowTreeStyleSettingsDialog();
                    break;
            }
        }

        private void OnPermissionsChanged()
        {
            RefreshRolePermissionsCache();
            RefreshMenuTree();
            RefreshOpenedPagesPermissionState();

            if (!string.IsNullOrWhiteSpace(_currentPageKey))
            {
                var mappedKey = _menuKeyMap.TryGetValue(_currentPageKey, out var menuKey) ? menuKey : _currentPageKey;
                if (!IsMenuKeyAllowed(mappedKey))
                {
                    CloseContentPage();
                    MessageBox.Show("当前页面权限已变更，页面已关闭。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void RefreshOpenedPagesPermissionState()
        {
            foreach (var page in _pages.Values)
            {
                switch (page)
                {
                    case SamplingRecordPage samplingPage:
                        samplingPage.RefreshPermissionState();
                        break;
                    case QualitySupervisionPage supervisionPage:
                        supervisionPage.RefreshPermissionState();
                        break;
                }
            }
        }

        /// <summary>
        /// 显示关于对话框
        /// </summary>
        private void ShowAboutDialog()
        {
            var dialog = new Window
            {
                Title = "关于",
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var layout = new StackPanel { Margin = new Thickness(10) };
            layout.Children.Add(new Label { Content = "软件名称：食品信息管理系统", Margin = new Thickness(0, 0, 0, 5) });
            layout.Children.Add(new Label { Content = $"当前版本：{_db.GetLatestVersion()}", Margin = new Thickness(0, 0, 0, 5) });
            layout.Children.Add(new Label { Content = "著作权人：王正发", Margin = new Thickness(0, 0, 0, 10) });
            
            var btnOk = new Button { Content = "确定", Width = 80, HorizontalAlignment = HorizontalAlignment.Center };
            btnOk.Click += (s, e) => dialog.Close();
            layout.Children.Add(btnOk);

            dialog.Content = layout;
            dialog.ShowDialog();
        }

        private void ShowChangePasswordDialog()
        {
            var dialog = new Window
            {
                Title = "修改密码",
                Width = 420,
                Height = 250,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var grid = new Grid { Margin = new Thickness(12) };
            for (var i = 0; i < 4; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            PasswordBox AddPassword(string label, int row)
            {
                var lbl = new TextBlock { Text = label, Margin = new Thickness(0, 8, 8, 8), VerticalAlignment = VerticalAlignment.Center };
                Grid.SetRow(lbl, row);
                Grid.SetColumn(lbl, 0);
                grid.Children.Add(lbl);

                var box = new PasswordBox { Margin = new Thickness(0, 8, 0, 8) };
                Grid.SetRow(box, row);
                Grid.SetColumn(box, 1);
                grid.Children.Add(box);
                return box;
            }

            var oldPwdBox = AddPassword("旧密码:", 0);
            var newPwdBox = AddPassword("新密码:", 1);
            var confirmPwdBox = AddPassword("确认新密码:", 2);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnSave = new Button { Content = "保存", Width = 86, Margin = new Thickness(0, 10, 8, 0) };
            var btnCancel = new Button { Content = "取消", Width = 86, Margin = new Thickness(0, 10, 0, 0) };

            btnSave.Click += (_, _) =>
            {
                var oldPwd = oldPwdBox.Password;
                var newPwd = newPwdBox.Password;
                var confirmPwd = confirmPwdBox.Password;

                if (string.IsNullOrWhiteSpace(oldPwd) || string.IsNullOrWhiteSpace(newPwd) || string.IsNullOrWhiteSpace(confirmPwd))
                {
                    MessageBox.Show("请完整填写密码项。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!string.Equals(newPwd, confirmPwd, StringComparison.Ordinal))
                {
                    MessageBox.Show("两次输入的新密码不一致。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (newPwd.Length < 6)
                {
                    MessageBox.Show("新密码长度至少为 6 位。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var changed = _db.ChangeUserPassword(_currentUserId, oldPwd, newPwd);
                    if (!changed)
                    {
                        MessageBox.Show("密码修改失败，请稍后重试。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    MessageBox.Show("密码修改成功。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    dialog.DialogResult = true;
                    dialog.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"修改密码失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            btnCancel.Click += (_, _) => dialog.Close();
            buttons.Children.Add(btnSave);
            buttons.Children.Add(btnCancel);
            Grid.SetRow(buttons, 5);
            Grid.SetColumn(buttons, 1);
            grid.Children.Add(buttons);

            dialog.Content = grid;
            dialog.ShowDialog();
        }

        private void Relogin()
        {
            var result = MessageBox.Show("确定重新登录吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                AuthService.MarkLogoutAsync(_currentUserId).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                WriteLog($"[Relogin] 回写离线状态失败: {ex.Message}");
            }

            var loginWindow = new 食品信息管理系统.Views.LoginWindow();
            Application.Current.MainWindow = loginWindow;
            loginWindow.Show();

            _isReloginInProgress = true;
            Close();
        }

        private void ShowThemeSettingsDialog()
        {
            var dialog = new Window
            {
                Title = "主题设置",
                Width = 430,
                Height = 250,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var root = new StackPanel { Margin = new Thickness(12) };
            root.Children.Add(new TextBlock { Text = "界面主题:", Margin = new Thickness(0, 0, 0, 8) });

            var combo = new ComboBox { Margin = new Thickness(0, 0, 0, 8) };
            combo.Items.Add("浅色");
            combo.Items.Add("深色");

            var currentTheme = ThemeConfigHelper.CfgGet(_db, "Settings", "theme_preference", "light");
            combo.SelectedIndex = string.Equals(currentTheme, "dark", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            root.Children.Add(combo);

            root.Children.Add(new TextBlock { Text = "天气城市:", Margin = new Thickness(0, 8, 0, 8) });
            var weatherCity = ThemeConfigHelper.CfgGet(_db, "Settings", WeatherCitySettingKey, "北京");
            var cityCombo = new ComboBox { Margin = new Thickness(0, 0, 0, 8), IsEditable = true, IsTextSearchEnabled = true };
            cityCombo.Items.Add("北京");
            cityCombo.Items.Add("上海");
            cityCombo.Items.Add("广州");
            cityCombo.Items.Add("深圳");
            cityCombo.Items.Add("杭州");
            cityCombo.Items.Add("南京");
            cityCombo.Items.Add("武汉");
            cityCombo.Items.Add("成都");
            cityCombo.Items.Add("西安");
            cityCombo.Items.Add("重庆");
            cityCombo.Text = weatherCity;
            root.Children.Add(cityCombo);

            root.Children.Add(new TextBlock { Text = "天气刷新(分钟):", Margin = new Thickness(0, 8, 0, 8) });
            var refreshMinutes = ThemeConfigHelper.CfgInt(_db, "Settings", WeatherRefreshMinutesSettingKey, 10);
            var refreshMinutesCombo = new ComboBox { Margin = new Thickness(0, 0, 0, 8), IsEditable = true, IsTextSearchEnabled = true };
            refreshMinutesCombo.Items.Add("1");
            refreshMinutesCombo.Items.Add("5");
            refreshMinutesCombo.Items.Add("10");
            refreshMinutesCombo.Items.Add("15");
            refreshMinutesCombo.Items.Add("30");
            refreshMinutesCombo.Items.Add("60");
            refreshMinutesCombo.Items.Add("120");
            refreshMinutesCombo.Text = refreshMinutes.ToString();
            root.Children.Add(refreshMinutesCombo);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnSave = new Button { Content = "保存", Width = 86, Margin = new Thickness(0, 10, 8, 0) };
            var btnCancel = new Button { Content = "取消", Width = 86, Margin = new Thickness(0, 10, 0, 0) };

            btnSave.Click += (_, _) =>
            {
                var isDark = combo.SelectedIndex == 1;
                var city = cityCombo.Text.Trim();
                if (string.IsNullOrWhiteSpace(city))
                {
                    MessageBox.Show("天气城市不能为空。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(refreshMinutesCombo.Text.Trim(), out var minutes) || minutes < 1 || minutes > 120)
                {
                    MessageBox.Show("天气刷新间隔范围应为 1~120 分钟。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ThemeConfigHelper.SaveConfig(_db, "Settings", "theme_preference", isDark ? "dark" : "light");
                ThemeConfigHelper.SaveConfig(_db, "Settings", WeatherCitySettingKey, city);
                ThemeConfigHelper.SaveConfig(_db, "Settings", WeatherRefreshMinutesSettingKey, minutes.ToString());
                ThemeManager.ApplyTheme(isDark ? ThemeType.Dark : ThemeType.Light);
                WeatherHelper.SetCity(city);
                _weatherRefreshTimer.Interval = minutes * 60_000;
                WeatherHelper.RefreshWeather();
                dialog.DialogResult = true;
                dialog.Close();
            };

            btnCancel.Click += (_, _) => dialog.Close();
            buttons.Children.Add(btnSave);
            buttons.Children.Add(btnCancel);
            root.Children.Add(buttons);

            dialog.Content = root;
            dialog.ShowDialog();
        }

        private void ShowTreeStyleSettingsDialog()
        {
            var currentRowHeight = ThemeConfigHelper.CfgInt(_db, "Settings", "tree_row_height", 26);
            var currentExpandLevel = ThemeConfigHelper.CfgInt(_db, "Settings", "tree_expand_level", 2);
            var currentIndent = ThemeConfigHelper.CfgInt(_db, "Settings", "tree_indent", 18);
            var currentIndicatorSize = ThemeConfigHelper.CfgInt(_db, "Settings", "indicator_size", 12);
            var currentHideRoot = ThemeConfigHelper.CfgBool(_db, "Settings", "hide_root_branch", false);
            var currentUseSysStyle = ThemeConfigHelper.CfgBool(_db, "Settings", "tree_classic_use_system", true);

            var dialog = new Window
            {
                Title = "树样式设置",
                Width = 450,
                Height = 360,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var grid = new Grid { Margin = new Thickness(12) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var rowHeightLabel = new TextBlock { Text = "树行高:", Margin = new Thickness(0, 8, 8, 8), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(rowHeightLabel, 0);
            Grid.SetColumn(rowHeightLabel, 0);
            grid.Children.Add(rowHeightLabel);

            var rowHeightCombo = new ComboBox { Margin = new Thickness(0, 8, 0, 8), IsEditable = true, IsTextSearchEnabled = true };
            rowHeightCombo.Items.Add("18");
            rowHeightCombo.Items.Add("22");
            rowHeightCombo.Items.Add("26");
            rowHeightCombo.Items.Add("30");
            rowHeightCombo.Items.Add("36");
            rowHeightCombo.Items.Add("44");
            rowHeightCombo.Items.Add("52");
            rowHeightCombo.Items.Add("64");
            rowHeightCombo.Items.Add("80");
            rowHeightCombo.Text = currentRowHeight.ToString();
            Grid.SetRow(rowHeightCombo, 0);
            Grid.SetColumn(rowHeightCombo, 1);
            grid.Children.Add(rowHeightCombo);

            var expandLabel = new TextBlock { Text = "默认展开层级:", Margin = new Thickness(0, 8, 8, 8), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(expandLabel, 1);
            Grid.SetColumn(expandLabel, 0);
            grid.Children.Add(expandLabel);

            var expandCombo = new ComboBox { Margin = new Thickness(0, 8, 0, 8), IsEditable = true, IsTextSearchEnabled = true };
            for (var level = 0; level <= 10; level++)
            {
                expandCombo.Items.Add(level.ToString());
            }
            expandCombo.Text = currentExpandLevel.ToString();
            Grid.SetRow(expandCombo, 1);
            Grid.SetColumn(expandCombo, 1);
            grid.Children.Add(expandCombo);

            var indentLabel = new TextBlock { Text = "树缩进:", Margin = new Thickness(0, 8, 8, 8), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(indentLabel, 2);
            Grid.SetColumn(indentLabel, 0);
            grid.Children.Add(indentLabel);

            var indentCombo = new ComboBox { Margin = new Thickness(0, 8, 0, 8), IsEditable = true, IsTextSearchEnabled = true };
            indentCombo.Items.Add("0");
            indentCombo.Items.Add("8");
            indentCombo.Items.Add("12");
            indentCombo.Items.Add("18");
            indentCombo.Items.Add("24");
            indentCombo.Items.Add("32");
            indentCombo.Items.Add("40");
            indentCombo.Items.Add("56");
            indentCombo.Items.Add("80");
            indentCombo.Text = currentIndent.ToString();
            Grid.SetRow(indentCombo, 2);
            Grid.SetColumn(indentCombo, 1);
            grid.Children.Add(indentCombo);

            var indicatorLabel = new TextBlock { Text = "指示器大小:", Margin = new Thickness(0, 8, 8, 8), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(indicatorLabel, 3);
            Grid.SetColumn(indicatorLabel, 0);
            grid.Children.Add(indicatorLabel);

            var indicatorCombo = new ComboBox { Margin = new Thickness(0, 8, 0, 8), IsEditable = true, IsTextSearchEnabled = true };
            indicatorCombo.Items.Add("8");
            indicatorCombo.Items.Add("10");
            indicatorCombo.Items.Add("12");
            indicatorCombo.Items.Add("14");
            indicatorCombo.Items.Add("16");
            indicatorCombo.Items.Add("20");
            indicatorCombo.Items.Add("24");
            indicatorCombo.Items.Add("32");
            indicatorCombo.Items.Add("40");
            indicatorCombo.Text = currentIndicatorSize.ToString();
            Grid.SetRow(indicatorCombo, 3);
            Grid.SetColumn(indicatorCombo, 1);
            grid.Children.Add(indicatorCombo);

            var hideRootCheck = new CheckBox { Content = "隐藏根分支", IsChecked = currentHideRoot, Margin = new Thickness(0, 8, 0, 8) };
            Grid.SetRow(hideRootCheck, 4);
            Grid.SetColumn(hideRootCheck, 1);
            grid.Children.Add(hideRootCheck);

            var useSystemCheck = new CheckBox { Content = "树经典样式使用系统外观", IsChecked = currentUseSysStyle, Margin = new Thickness(0, 8, 0, 8) };
            Grid.SetRow(useSystemCheck, 5);
            Grid.SetColumn(useSystemCheck, 1);
            grid.Children.Add(useSystemCheck);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnSave = new Button { Content = "保存", Width = 86, Margin = new Thickness(0, 10, 8, 0) };
            var btnCancel = new Button { Content = "取消", Width = 86, Margin = new Thickness(0, 10, 0, 0) };

            btnSave.Click += (_, _) =>
            {
                if (!int.TryParse(rowHeightCombo.Text.Trim(), out var rowHeight) || rowHeight < 18 || rowHeight > 80)
                {
                    MessageBox.Show("树行高范围应为 18~80。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(expandCombo.Text.Trim(), out var expandLevel) || expandLevel < 0 || expandLevel > 10)
                {
                    MessageBox.Show("展开层级范围应为 0~10。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(indentCombo.Text.Trim(), out var indent) || indent < 0 || indent > 80)
                {
                    MessageBox.Show("树缩进范围应为 0~80。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(indicatorCombo.Text.Trim(), out var indicatorSize) || indicatorSize < 8 || indicatorSize > 40)
                {
                    MessageBox.Show("指示器大小范围应为 8~40。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ThemeConfigHelper.SaveConfig(_db, "Settings", "tree_row_height", rowHeight.ToString());
                ThemeConfigHelper.SaveConfig(_db, "Settings", "tree_expand_level", expandLevel.ToString());
                ThemeConfigHelper.SaveConfig(_db, "Settings", "tree_indent", indent.ToString());
                ThemeConfigHelper.SaveConfig(_db, "Settings", "indicator_size", indicatorSize.ToString());
                ThemeConfigHelper.SaveConfig(_db, "Settings", "hide_root_branch", hideRootCheck.IsChecked == true ? "1" : "0");
                ThemeConfigHelper.SaveConfig(_db, "Settings", "tree_classic_use_system", useSystemCheck.IsChecked == true ? "1" : "0");

                ApplyMainTreeVisualConfig();
                ExpandMainTreeLevel(expandLevel);

                dialog.DialogResult = true;
                dialog.Close();
            };

            btnCancel.Click += (_, _) => dialog.Close();
            buttons.Children.Add(btnSave);
            buttons.Children.Add(btnCancel);
            Grid.SetRow(buttons, 3);
            Grid.SetColumn(buttons, 1);
            grid.Children.Add(buttons);

            dialog.Content = grid;
            dialog.ShowDialog();
        }

        private void ApplyThemePreference()
        {
            try
            {
                var theme = ThemeConfigHelper.CfgGet(_db, "Settings", "theme_preference", "light");
                var target = string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? ThemeType.Dark : ThemeType.Light;
                ThemeManager.ApplyTheme(target);
            }
            catch
            {
                ThemeManager.ApplyThemeSafe(Application.Current);
            }
        }

        private void InitWeatherStatus()
        {
            WeatherHelper.WeatherUpdated -= OnWeatherUpdated;
            WeatherHelper.WeatherError -= OnWeatherError;
            WeatherHelper.WeatherUpdated += OnWeatherUpdated;
            WeatherHelper.WeatherError += OnWeatherError;

            var city = ThemeConfigHelper.CfgGet(_db, "Settings", WeatherCitySettingKey, "北京");
            WeatherHelper.SetCity(city);
            LblWeather.Content = $"天气：{city} 加载中";
            WeatherHelper.RefreshWeather();
        }

        private void OnWeatherUpdated(object data)
        {
            Dispatcher.Invoke(() =>
            {
                _lastWeatherUpdatedAt = DateTime.Now;
                LblWeather.Content = $"天气：{WeatherHelper.FormatWeatherDisplay(data)} · 更新{_lastWeatherUpdatedAt:HH:mm}";
            });
        }

        private void OnWeatherError(string message)
        {
            WriteLog($"[Weather] {message}");
            Dispatcher.Invoke(() =>
            {
                var shortMessage = NormalizeWeatherError(message);
                if (_lastWeatherUpdatedAt.HasValue)
                {
                    LblWeather.Content = $"天气：{shortMessage} · 最近更新{_lastWeatherUpdatedAt:HH:mm}";
                }
                else
                {
                    LblWeather.Content = $"天气：{shortMessage}";
                }
            });
        }

        private static string NormalizeWeatherError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "刷新失败";
            }

            var text = message.Trim();
            const string prefix = "天气刷新失败:";
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(prefix.Length).Trim();
            }

            if (text.Length > 24)
            {
                text = text.Substring(0, 24) + "...";
            }

            return $"刷新失败({text})";
        }

        private double GetWeatherRefreshIntervalMs()
        {
            var minutes = ThemeConfigHelper.CfgInt(_db, "Settings", WeatherRefreshMinutesSettingKey, 10);
            minutes = Math.Max(1, Math.Min(120, minutes));
            return minutes * 60_000;
        }

        /// <summary>
        /// 检查数据库连接
        /// </summary>
        private void CheckDbConnection()
        {
            try
            {
                if (!_db.CheckConnection())
                {
                    _db.Reconnect();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据库连接检查失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 空闲超时处理
        /// </summary>
        private void OnIdleTimeout()
        {
            if (!_idleExitEnabled || _isAutoExitInProgress)
            {
                return;
            }

            var now = DateTime.Now;
            var idleTime = (now - _lastActivityTime).TotalMilliseconds;
            if (idleTime <= _idleExitTimeoutMs)
            {
                return;
            }

            _isAutoExitInProgress = true;
            _idleExitEnabled = false;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var app = Application.Current;
                    if (app != null)
                    {
                        app.Shutdown();
                    }
                    else
                    {
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    WriteLog($"[OnIdleTimeout] 自动退出失败: {ex.Message}");
                    _isAutoExitInProgress = false;
                    _idleExitEnabled = true;
                }
            }));
        }

        /// <summary>
        /// 刷新菜单树
        /// </summary>
        private void RefreshMenuTree()
        {
            try
            {
                RefreshRolePermissionsCache();
                var menuList = _db.GetMenuList();

                _menuTree.BeginUpdate();
                _menuTree.Nodes.Clear();
                BuildMainMenuTree(menuList);
                BuildMenuKeyMap();
                ApplyMenuPermissionsToTree();
                ApplyMainTreeVisualConfig();

                var expandLevel = ThemeConfigHelper.CfgInt(_db, "Settings", "tree_expand_level", 2);
                ExpandMainTreeLevel(expandLevel);
                _menuTree.EndUpdate();
            }
            catch (Exception ex)
            {
                WriteLog($"[RefreshMenuTree] 错误: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void BuildMainMenuTree(List<Dictionary<string, object>> menuList)
        {
            if (menuList == null || menuList.Count == 0)
            {
                return;
            }

            var groups = menuList
                .Where(m => m != null)
                .GroupBy(m => GetString(m, "parent_key"))
                .ToDictionary(g => g.Key, g => g.OrderBy(item => GetInt(item, "sort_order")).ToList());

            var roots = groups.TryGetValue(string.Empty, out var rootList)
                ? rootList
                : menuList.Where(m => string.IsNullOrEmpty(GetString(m, "parent_key"))).ToList();

            foreach (var menu in roots)
            {
                var node = CreateMenuTreeNode(menu, groups);
                if (node != null)
                {
                    _menuTree.Nodes.Add(node);
                }
            }
        }

        private static WF.TreeNode? CreateMenuTreeNode(Dictionary<string, object> menu, Dictionary<string, List<Dictionary<string, object>>> groups)
        {
            var key = GetString(menu, "menu_key");
            var title = GetString(menu, "title");
            if (string.IsNullOrWhiteSpace(key) && string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            var node = new WF.TreeNode
            {
                Text = string.IsNullOrWhiteSpace(title) ? key : title,
                Tag = key
            };

            if (groups.TryGetValue(key, out var children))
            {
                foreach (var child in children)
                {
                    var childNode = CreateMenuTreeNode(child, groups);
                    if (childNode != null)
                    {
                        node.Nodes.Add(childNode);
                    }
                }
            }

            return node;
        }

        private static string GetString(Dictionary<string, object> dict, string key)
        {
            if (dict == null) return string.Empty;
            return dict.TryGetValue(key, out var value) && value != null ? value.ToString() ?? string.Empty : string.Empty;
        }

        private static int GetInt(Dictionary<string, object> dict, string key)
        {
            if (dict == null) return 0;
            var s = GetString(dict, key);
            return int.TryParse(s, out var v) ? v : 0;
        }

        private void ExpandMainTreeLevel(int level)
        {
            if (level <= 0)
            {
                _menuTree.CollapseAll();
                return;
            }

            if (level >= 2)
            {
                _menuTree.ExpandAll();
                return;
            }

            foreach (WF.TreeNode node in _menuTree.Nodes)
            {
                node.Expand();
                foreach (WF.TreeNode child in node.Nodes)
                {
                    child.Collapse();
                }
            }
        }

        private void ApplyMainTreeVisualConfig()
        {
            var indent = ThemeConfigHelper.CfgInt(_db, "Settings", "tree_indent", 18);
            var rowHeight = ThemeConfigHelper.CfgInt(_db, "Settings", "tree_row_height", 22);
            var hideRootBranch = ThemeConfigHelper.CfgBool(_db, "Settings", "hide_root_branch", false);
            var useSysStyle = ThemeConfigHelper.CfgBool(_db, "Settings", "tree_classic_use_system", true);

            _menuTree.Indent = Math.Max(10, Math.Min(48, indent));
            _menuTree.ItemHeight = Math.Max(18, Math.Min(48, rowHeight));
            _menuTree.ShowRootLines = !hideRootBranch;

            if (useSysStyle)
            {
                _menuTree.BackColor = System.Drawing.SystemColors.Window;
                _menuTree.ForeColor = System.Drawing.SystemColors.WindowText;
            }
        }

        /// <summary>
        /// 恢复窗口初始尺寸
        /// </summary>
        private void RestoreInitialGeometry()
        {
            var uiConfig = UiHelper.GetUiConfig(_db);
            if (uiConfig == null || !uiConfig.ContainsKey("main_window_geometry")) return;

            try
            {
                var geometry = uiConfig["main_window_geometry"] as Dictionary<string, object>;
                if (geometry == null) return;

                var x = ReadInt(geometry, "x", 80);
                var y = ReadInt(geometry, "y", 80);
                var width = ReadInt(geometry, "w", 1280);
                var height = ReadInt(geometry, "h", 800);
                var maximized = ReadBool(geometry, "maximized", false);

                // 验证尺寸合法性
                var screen = System.Windows.SystemParameters.WorkArea;
                x = (int)Math.Max(screen.X, Math.Min(x, screen.Right - width));
                y = (int)Math.Max(screen.Y, Math.Min(y, screen.Bottom - height));
                width = (int)Math.Max(MinWidth, Math.Min(width, screen.Width));
                height = (int)Math.Max(MinHeight, Math.Min(height, screen.Height));

                // 应用尺寸
                Left = x;
                Top = y;
                Width = width;
                Height = height;

                if (maximized)
                {
                    WindowState = WindowState.Maximized;
                }

                if (uiConfig.TryGetValue("main_window_splitter_sizes", out var splitObj) && splitObj is List<object> splitList && splitList.Count > 0)
                {
                    var treeWidth = ReadDouble(splitList[0], 0);
                    if (treeWidth > 0)
                    {
                        _expandedTreePanelWidth = ClampTreePanelWidth(treeWidth);
                        ColTreePanel.Width = new GridLength(_expandedTreePanelWidth);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"恢复窗口尺寸失败：{ex.Message}");
            }
        }

        private static double ClampTreePanelWidth(double width)
        {
            if (double.IsNaN(width) || double.IsInfinity(width))
            {
                return DefaultTreePanelWidth;
            }

            return Math.Max(MinTreePanelWidth, Math.Min(width, MaxTreePanelWidth));
        }

        private static int ReadInt(Dictionary<string, object> map, string key, int fallback)
        {
            if (!map.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return int.TryParse(value.ToString(), out var result) ? result : fallback;
        }

        private static bool ReadBool(Dictionary<string, object> map, string key, bool fallback)
        {
            if (!map.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            return bool.TryParse(value.ToString(), out var result) ? result : fallback;
        }

        private static double ReadDouble(object? value, double fallback)
        {
            if (value == null)
            {
                return fallback;
            }

            return double.TryParse(value.ToString(), out var result) ? result : fallback;
        }

        private void SaveCurrentGeometryConfig()
        {
            try
            {
                var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
                var geometry = new JObject
                {
                    ["x"] = (int)bounds.Left,
                    ["y"] = (int)bounds.Top,
                    ["w"] = (int)bounds.Width,
                    ["h"] = (int)bounds.Height,
                    ["maximized"] = WindowState == WindowState.Maximized
                };

                var contentWidth = Math.Max(0, (int)(ActualWidth - 20 - ColTreePanel.ActualWidth));
                var splitSizes = new JArray
                {
                    (int)Math.Max(0, ColTreePanel.ActualWidth),
                    contentWidth
                };

                _db.SetSystemConfig("ui", "main_window_geometry", geometry.ToString(Newtonsoft.Json.Formatting.None));
                _db.SetSystemConfig("ui", "main_window_splitter_sizes", splitSizes.ToString(Newtonsoft.Json.Formatting.None));
            }
            catch (Exception ex)
            {
                WriteLog($"[SaveCurrentGeometryConfig] 保存窗口布局失败: {ex.Message}");
            }
        }

        #region 权限相关
        /// <summary>
        /// 刷新角色权限缓存
        /// </summary>
        private void RefreshRolePermissionsCache()
        {
            _rolePermissionKeys = _db.GetRolePermissions(_currentRole);
        }

        /// <summary>
        /// 构建菜单Key映射
        /// </summary>
        private void BuildMenuKeyMap()
        {
            _menuKeyMap = _db.GetMenuKeyMap();
        }

        /// <summary>
        /// 检查菜单Key是否有权限
        /// </summary>
        private bool IsMenuKeyAllowed(string menuKey)
        {
            if (_currentRole == 1)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(menuKey))
            {
                return false;
            }

            return _rolePermissionKeys != null && _rolePermissionKeys.Contains(menuKey);
        }

        private void ApplyMenuPermissionsToTree()
        {
            if (_currentRole == 1)
            {
                return;
            }

            var toRemove = new List<WF.TreeNode>();
            foreach (WF.TreeNode node in _menuTree.Nodes)
            {
                if (!ApplyPermissionToItem(node))
                {
                    toRemove.Add(node);
                }
            }

            foreach (var node in toRemove)
            {
                _menuTree.Nodes.Remove(node);
            }
        }

        private bool ApplyPermissionToItem(WF.TreeNode item)
        {
            var hasAllowedChild = false;
            var childRemove = new List<WF.TreeNode>();
            foreach (WF.TreeNode child in item.Nodes)
            {
                if (ApplyPermissionToItem(child))
                {
                    hasAllowedChild = true;
                }
                else
                {
                    childRemove.Add(child);
                }
            }

            foreach (var child in childRemove)
            {
                item.Nodes.Remove(child);
            }

            var rawKey = item.Tag?.ToString() ?? string.Empty;
            var menuKey = _menuKeyMap.TryGetValue(rawKey, out var mapped) ? mapped : rawKey;
            var allowed = IsMenuKeyAllowed(menuKey);
            var visible = allowed || hasAllowedChild;
            return visible;
        }
        #endregion

        #region 配置相关
        #endregion
        #endregion
    }
}