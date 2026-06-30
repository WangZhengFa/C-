using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FoodEnterpriseIMS.Themes
{
    public static class ThemeManager
    {
        private static ThemeType _currentTheme = ThemeType.Light;
        private static ResourceDictionary _darkThemeDict;
        private static ResourceDictionary _lightThemeDict;

        /// <summary>
        /// 当前激活的主题
        /// </summary>
        public static ThemeType CurrentTheme => _currentTheme;

        static ThemeManager()
        {
            // 加载主题资源字典
            _darkThemeDict = Application.LoadComponent(new Uri("/Themes/ThemeStyles.xaml", UriKind.Relative)) as ResourceDictionary;
            _lightThemeDict = _darkThemeDict["LightTheme"] as ResourceDictionary;
            _darkThemeDict = _darkThemeDict["DarkTheme"] as ResourceDictionary;
        }

        /// <summary>
        /// 应用主题到整个应用程序
        /// </summary>
        /// <param name="theme">目标主题</param>
        public static void ApplyTheme(ThemeType theme)
        {
            // 移除旧主题
            var oldThemeDict = _currentTheme == ThemeType.Dark ? _darkThemeDict : _lightThemeDict;
            if (Application.Current.Resources.MergedDictionaries.Contains(oldThemeDict))
            {
                Application.Current.Resources.MergedDictionaries.Remove(oldThemeDict);
            }

            // 应用新主题
            _currentTheme = theme;
            var newThemeDict = theme == ThemeType.Dark ? _darkThemeDict : _lightThemeDict;
            Application.Current.Resources.MergedDictionaries.Add(newThemeDict);

            // 刷新所有窗口样式
            foreach (Window window in Application.Current.Windows)
            {
                RefreshWindowStyle(window);
            }
        }

        /// <summary>
        /// 为控件设置角色样式（对应原Qt的set_role）
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="role">控件角色</param>
        public static void SetControlRole(FrameworkElement control, ControlRole role)
        {
            if (control == null) return;

            switch (role)
            {
                case ControlRole.Primary:
                    control.Style = _currentTheme == ThemeType.Dark 
                        ? _darkThemeDict["DefaultButtonStyle"] as Style 
                        : _lightThemeDict["DefaultButtonStyle"] as Style;
                    break;
                case ControlRole.Secondary:
                    control.Style = _currentTheme == ThemeType.Dark 
                        ? _darkThemeDict["SecondaryButtonStyle"] as Style 
                        : _lightThemeDict["SecondaryButtonStyle"] as Style;
                    break;
                case ControlRole.Danger:
                    control.Style = _currentTheme == ThemeType.Dark 
                        ? _darkThemeDict["DangerButtonStyle"] as Style 
                        : _lightThemeDict["DangerButtonStyle"] as Style;
                    break;
                case ControlRole.TableButton:
                    control.Style = _currentTheme == ThemeType.Dark 
                        ? _darkThemeDict["TableButtonStyle"] as Style 
                        : _lightThemeDict["TableButtonStyle"] as Style;
                    break;
                // 其他角色可按需扩展
            }
        }

        /// <summary>
        /// 刷新窗口样式（解决主题切换后样式不生效问题）
        /// </summary>
        /// <param name="window">目标窗口</param>
        private static void RefreshWindowStyle(Window window)
        {
            if (window == null) return;

            // 重新应用样式
            var originalStyle = window.Style;
            window.Style = null;
            window.Style = originalStyle;

            // 递归刷新子控件
            RefreshControlStyle(window);
        }

        /// <summary>
        /// 递归刷新控件样式
        /// </summary>
        /// <param name="control">目标控件</param>
        private static void RefreshControlStyle(FrameworkElement control)
        {
            if (control == null) return;

            // 重新应用样式
            var originalStyle = control.Style;
            control.Style = null;
            control.Style = originalStyle;

            // 处理子控件
            if (control is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is FrameworkElement fe)
                        RefreshControlStyle(fe);
                }
            }
            else if (control is ContentControl contentControl && contentControl.Content is FrameworkElement contentFe)
            {
                RefreshControlStyle(contentFe);
            }
        }

        /// <summary>
        /// 解析主题偏好（从配置/环境变量读取）
        /// </summary>
        /// <returns>解析后的主题</returns>
        public static ThemeType ResolveThemePreference()
        {
            // 1. 读取环境变量
            var envTheme = Environment.GetEnvironmentVariable("LAUNCHER_THEME")?.ToLower();
            if (envTheme == "dark") return ThemeType.Dark;
            if (envTheme == "light") return ThemeType.Light;

            // 2. 读取配置文件（示例：ini配置）
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher_config.ini");
            if (File.Exists(configPath))
            {
                // 实际项目中可使用IniParser等库读取
                // 此处简化逻辑，默认返回Light
            }

            // 默认浅色主题
            return ThemeType.Light;
        }

        /// <summary>
        /// 设置树控件行高
        /// </summary>
        /// <param name="treeView">目标树控件</param>
        /// <param name="rowHeight">行高</param>
        public static void SetTreeViewRowHeight(TreeView treeView, double rowHeight)
        {
            if (treeView == null) return;

            var treeItemStyle = new Style(typeof(TreeViewItem), treeView.ItemContainerStyle);
            treeItemStyle.Setters.Add(new Setter(FrameworkElement.HeightProperty, rowHeight));
            treeView.ItemContainerStyle = treeItemStyle;
        }

        /// <summary>
        /// 安全应用主题（捕获异常，防止启动时因资源缺失崩溃）
        /// </summary>
        public static void ApplyThemeSafe(Application app)
        {
            try
            {
                ApplyTheme(ResolveThemePreference());
            }
            catch
            {
                // 主题资源未就绪时静默失败，使用系统默认样式
            }
        }
    }
}
