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
using 食品信息管理系统.Services;
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
        private readonly int _defaultLeftCollapsedWidth = 120;
        private List<string> _rolePermissionKeys;
        private Dictionary<string, string> _menuKeyMap = new Dictionary<string, string>();
        private string _currentPageKey;
        private int? _pendingTreeSelection;
        private Dictionary<string, UIElement> _pages = new Dictionary<string, UIElement>();
        private bool _treeContextMenuAttached;
        private const string MenuTreeKey = "system_menu";

        // 定时器
        private readonly Timer _reconnectTimer = new Timer(60000); // 1分钟检查一次数据库连接
        private readonly Timer _idleTimer = new Timer();
        private readonly Timer _dbStatusTimer = new Timer(30000);
        private readonly Timer _weatherRefreshTimer = new Timer(10 * 60 * 1000);
        private DateTime _lastActivityTime;
        private bool _idleExitEnabled = false;
        private int _idleExitTimeoutMs = 15 * 60 * 1000; // 15分钟
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
            _idleExitEnabled = true;
            _lastActivityTime = DateTime.Now;
            _idleTimer.Start();

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
            var dateTimer = new Timer(1000);
            dateTimer.Elapsed += (s, e) => Dispatcher.Invoke(UpdateDateTime);
            dateTimer.Start();
            
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

            WeatherHelper.WeatherUpdated -= OnWeatherUpdated;
            WeatherHelper.WeatherError -= OnWeatherError;
            _weatherRefreshTimer.Stop();

            var confirmWindow = new ConfirmExitWindow { Owner = this };
            var result = confirmWindow.ShowDialog();
            if (result != true)
            {
                e.Cancel = true;
                    return;
            }

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

                if (new[] { "relogin", "exit_system", "change_password", "about", "theme_settings", "tree_style_settings", "user_management" }.Contains(pageKey))
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
                }
                else if (!string.IsNullOrWhiteSpace(componentPath))
                {
                    var className = componentPath.Contains(".")
                        ? componentPath
                        : $"食品信息管理系统.Views.Pages.{componentPath}";
                    page = CreatePageByReflection(className, title);
                    AttachCloseEvent(page);
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
                case "user_management":
                    var win = new 食品信息管理系统.Views.UserManagementWindow(_db, _currentRole)
                    {
                        Owner = this
                    };
                    win.PermissionsChanged += (_, _) => OnPermissionsChanged();
                    win.ShowDialog();
                    OnPermissionsChanged();
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

                TreeStyleHelper.ApplyGlobalTreeStyle(TreeMenu, _db);
                TreeStyleHelper.ExpandTreeLevel(TreeMenu, expandLevel);

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
                RefreshRolePermissionsCache();
                var menuList = _db.GetMenuList();
                TreeHelper.BuildMenuTree(TreeMenu, menuList, _currentRole);
                BuildMenuKeyMap();
                ApplyMenuPermissionsToTree();
                TreeStyleHelper.ApplyGlobalTreeStyle(TreeMenu, _db);
                var expandLevel = ThemeConfigHelper.CfgInt(_db, "Settings", "tree_expand_level", 2);
                TreeStyleHelper.ExpandTreeLevel(TreeMenu, expandLevel);
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
                        ColTreePanel.Width = new GridLength(treeWidth);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"恢复窗口尺寸失败：{ex.Message}");
            }
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

            foreach (var raw in TreeMenu.Items)
            {
                if (raw is TreeViewItem item)
                {
                    ApplyPermissionToItem(item);
                }
            }
        }

        private bool ApplyPermissionToItem(TreeViewItem item)
        {
            var hasAllowedChild = false;
            foreach (var raw in item.Items)
            {
                if (raw is TreeViewItem child && ApplyPermissionToItem(child))
                {
                    hasAllowedChild = true;
                }
            }

            var rawKey = item.Tag?.ToString() ?? string.Empty;
            var menuKey = _menuKeyMap.TryGetValue(rawKey, out var mapped) ? mapped : rawKey;
            var allowed = IsMenuKeyAllowed(menuKey);
            var visible = allowed || hasAllowedChild;

            item.Visibility = visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            return visible;
        }
        #endregion

        #region 配置相关
        #endregion
        #endregion
    }
}