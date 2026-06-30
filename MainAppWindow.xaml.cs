using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FoodEnterpriseIMS.Helpers;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;
using FoodEnterpriseIMS.Themes;
using FoodEnterpriseIMS.TreeCore;
using FoodEnterpriseIMS.Widgets;
using MySqlConnector;
using 食品信息管理系统.Views.Pages;
using 食品信息管理系统.Views.Dialogs;
using Timer = System.Timers.Timer;

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
        private readonly int _defaultLeftCollapsedWidth = 120;
        private List<string> _rolePermissionKeys;
        private Dictionary<string, string> _menuKeyMap = new Dictionary<string, string>();
        private string _currentPageKey;
        private int? _pendingTreeSelection;
        private Dictionary<string, UIElement> _pages = new Dictionary<string, UIElement>();
        private bool _fastExitRequested;
        private bool _treeContextMenuAttached;
        private const string MenuTreeKey = "system_menu";

        // 定时器
        private readonly Timer _reconnectTimer = new Timer(60000); // 1分钟检查一次数据库连接
        private readonly Timer _idleTimer = new Timer();
        private readonly Timer _dbStatusTimer = new Timer(30000);
        private DateTime _lastActivityTime;
        private bool _idleExitEnabled = false;
        private int _idleExitTimeoutMs = 15 * 60 * 1000; // 15分钟
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
            _currentRole = _config.ContainsKey("role_id") ? Convert.ToInt32(_config["role_id"]) : 0;

            // 初始化字体
            FontManager.RegisterFonts();
            
            // 应用主题
            ThemeManager.ApplyThemeSafe(Application.Current);
            
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
            _idleExitEnabled = true;
            _lastActivityTime = DateTime.Now;
            _idleTimer.Start();

            // 数据库状态检查定时器
            _dbStatusTimer.Elapsed += (s, e) => CheckDbStatus();
            _dbStatusTimer.Start();
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
            var dateTimer = new Timer(1000);
            dateTimer.Elapsed += (s, e) => Dispatcher.Invoke(UpdateDateTime);
            dateTimer.Start();
            
            // 初始化数据库状态
            CheckDbStatus();
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
            var confirmWindow = new ConfirmExitWindow { Owner = this };
            var result = confirmWindow.ShowDialog();
            if (result != true)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 切换树面板显示/隐藏
        /// </summary>
        private void ToggleTreePanel_Click(object sender, MouseButtonEventArgs e)
        {
            _leftCollapsed = !_leftCollapsed;
            ColTreePanel.Width = _leftCollapsed ? new GridLength(0) : new GridLength(_defaultLeftCollapsedWidth);
            LblToggleIcon.Content = _leftCollapsed ? ">>" : "<<";
        }

        /// <summary>
        /// 树节点点击事件
        /// </summary>
        private void TreeMenu_ItemClicked(object sender, RoutedEventArgs e)
        {
            // 从事件源向上查找 TreeViewItem
            var item = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
            if (item == null)
                return;

            var pageKey = item.Tag?.ToString();
            if (!string.IsNullOrEmpty(pageKey))
            {
                OpenPageByKey(pageKey);
            }
        }

        /// <summary>
        /// 树节点双击事件
        /// </summary>
        private void TreeMenu_ItemDoubleClicked(object sender, RoutedEventArgs e)
        {
            TreeMenu_ItemClicked(sender, e);
        }

        /// <summary>
        /// 向上查找指定类型的祖先元素
        /// </summary>
        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T ancestor)
                    return ancestor;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>
        /// 用户活动事件（重置空闲计时器）
        /// </summary>
        private void OnUserActivity(object sender, EventArgs e)
        {
            _lastActivityTime = DateTime.Now;
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

            TreeMenu.NodeAddRequested += TreeMenu_NodeAddRequested;
            TreeMenu.NodeEditRequested += TreeMenu_NodeEditRequested;
            TreeMenu.NodeDeleteRequested += TreeMenu_NodeDeleteRequested;
            TreeMenu.NodeExpandRequested += TreeMenu_NodeExpandRequested;
            TreeMenu.NodeCollapseRequested += TreeMenu_NodeCollapseRequested;
        }

        /// <summary>
        /// 新增节点
        /// </summary>
        private void TreeMenu_NodeAddRequested(object sender, RoutedEventArgs e)
        {
            var parentNode = (e as TreeNodeRoutedEventArgs)?.Node ?? e.Source as TreeNode;
            var newNode = new TreeNode
            {
                ParentCode = parentNode?.Code,
                SortOrder = 0
            };
            OpenNodeEditWindow(newNode, MenuTreeKey, isNew: true);
        }

        /// <summary>
        /// 编辑节点
        /// </summary>
        private void TreeMenu_NodeEditRequested(object sender, RoutedEventArgs e)
        {
            var node = (e as TreeNodeRoutedEventArgs)?.Node ?? e.Source as TreeNode;
            if (node == null || string.IsNullOrWhiteSpace(node.Code)) return;
            OpenNodeEditWindow(node, MenuTreeKey, isNew: false);
        }

        /// <summary>
        /// 删除节点
        /// </summary>
        private void TreeMenu_NodeDeleteRequested(object sender, RoutedEventArgs e)
        {
            var node = (e as TreeNodeRoutedEventArgs)?.Node ?? e.Source as TreeNode;
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
        private void TreeMenu_NodeExpandRequested(object sender, RoutedEventArgs e)
        {
            if (TreeMenu.SelectedItem is TreeViewItem item)
            {
                item.IsExpanded = true;
            }
        }

        /// <summary>
        /// 折叠节点
        /// </summary>
        private void TreeMenu_NodeCollapseRequested(object sender, RoutedEventArgs e)
        {
            if (TreeMenu.SelectedItem is TreeViewItem item)
            {
                item.IsExpanded = false;
            }
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
                var compPath = menuItem.ContainsKey("component_path") ? menuItem["component_path"].ToString() : "";
                var csharpClass = menuItem.ContainsKey("csharp_class") ? menuItem["csharp_class"].ToString() : "";
                if (!string.IsNullOrEmpty(csharpClass) || pageKey == "sample_record" || pageKey == "quality_supervision" ||
                    (!string.IsNullOrEmpty(compPath) && compPath.Contains(".")))
                {
                    ShowContentPage(pageKey, title, csharpClass);
                }
                else if (new[] { "relogin", "exit_system", "change_password", "about", "theme_settings", "tree_style_settings" }.Contains(pageKey))
                {
                    HandleSpecialActions(pageKey);
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
        private void ShowContentPage(string pageKey, string title, string? csharpClass = null)
        {
            // 页面创建逻辑
            if (!_pages.ContainsKey(pageKey))
            {
                UIElement page;
                if (!string.IsNullOrWhiteSpace(csharpClass))
                {
                    page = CreatePageByReflection(csharpClass, title);
                    AttachCloseEvent(page);
                }
                else
                {
                    switch (pageKey)
                    {
                        case "sample_record":
                            var samplingPage = new SamplingRecordPage();
                            samplingPage.CloseRequested += (s, e) => CloseContentPage();
                            page = samplingPage;
                            break;
                        case "quality_supervision":
                            var supervisionPage = new QualitySupervisionPage();
                            supervisionPage.CloseRequested += (s, e) => CloseContentPage();
                            page = supervisionPage;
                            break;
                        default:
                            page = new TextBlock { Text = $"页面：{title}", FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                            break;
                    }
                }
                _pages.Add(pageKey, page);
            }

            LblPageTitle.Content = title;

            // 切换内容
            ContentArea.Items.Clear();
            ContentArea.Items.Add(new TabItem { Content = _pages[pageKey], Header = title });
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
                    var instance = Activator.CreateInstance(type);
                    if (instance is UIElement uiElement)
                    {
                        return uiElement;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"[CreatePageByReflection] 反射创建页面失败 {csharpClass}: {ex.Message}");
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

        /// <summary>
        /// 关闭当前内容页
        /// </summary>
        private void CloseContentPage()
        {
            ContentArea.Items.Clear();
            LblPageTitle.Content = "";
        }

        /// <summary>
        /// 处理特殊操作（退出、改密码等）
        /// </summary>
        private void HandleSpecialActions(string actionKey)
        {
            switch (actionKey)
            {
                case "exit_system":
                    Application.Current.Shutdown();
                    break;
                case "about":
                    ShowAboutDialog();
                    break;
                case "change_password":
                    // 打开改密码对话框
                    break;
                case "relogin":
                    // 重新登录逻辑
                    break;
                case "theme_settings":
                    // 主题设置
                    break;
                case "tree_style_settings":
                    // 树样式设置
                    break;
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
            if (!_idleExitEnabled) return;

            var idleTime = (DateTime.Now - _lastActivityTime).TotalMilliseconds;
            if (idleTime > _idleExitTimeoutMs)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("长时间未操作，系统将自动退出", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    Application.Current.Shutdown();
                });
            }
        }

        /// <summary>
        /// 刷新菜单树
        /// </summary>
        private void RefreshMenuTree()
        {
            try
            {
                var menuList = _db.GetMenuList();
                TreeHelper.BuildMenuTree(TreeMenu, menuList, _currentRole);
            }
            catch (Exception ex)
            {
                WriteLog($"[RefreshMenuTree] 错误: {ex.Message}\n{ex.StackTrace}");
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

                var x = Convert.ToInt32(geometry["x"]);
                var y = Convert.ToInt32(geometry["y"]);
                var width = Convert.ToInt32(geometry["w"]);
                var height = Convert.ToInt32(geometry["h"]);
                var maximized = Convert.ToBoolean(geometry["maximized"]);

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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"恢复窗口尺寸失败：{ex.Message}");
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
            // 权限列表为空时默认放行（开发/默认角色），后续接入真实权限后移除
            return _rolePermissionKeys == null || _rolePermissionKeys.Count == 0 || _rolePermissionKeys.Contains(menuKey);
        }
        #endregion

        #region 配置相关
        #endregion
        #endregion
    }
}