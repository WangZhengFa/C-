using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FoodEnterpriseIMS.Services;

namespace FoodEnterpriseIMS.Themes
{
    /// <summary>
    /// 全局主题事件过滤器，对应 _ThemeEventFilter
    /// 窗口加载自动修补深色样式、树样式自动加载
    /// </summary>
    public class ThemeEventFilter : IWeakEventListener
    {
        private readonly ThemeType _theme;
        private readonly DatabaseManager _db;

        public ThemeEventFilter(ThemeType theme, DatabaseManager db)
        {
            _theme = theme;
            _db = db;
        }

        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (e is RoutedEventArgs rea && rea.RoutedEvent == FrameworkElement.LoadedEvent)
            {
                ProcessWindow(sender as Window);
            }
            return true;
        }

        private void ProcessWindow(Window? win)
        {
            if (win == null) return;

            // 1. 深色样式补丁：修正某些控件在深色主题下的背景
            if (_theme == ThemeType.Dark)
            {
                foreach (var elem in FindVisualChildren<Control>(win))
                {
                    if (elem.Style?.Resources is ResourceDictionary resources)
                    {
                        // 简单示例：如有 Light 专属资源，可在此替换为 Dark 版本
                    }
                }
            }

            // 2. 自动为树控件应用主题样式
            foreach (var tree in FindVisualChildren<TreeView>(win))
            {
                TreeStyleHelper.ApplyGlobalTreeStyle(tree, _db);
            }
        }

        /// <summary>
        /// 递归查找视觉树子元素
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                {
                    yield return t;
                }

                foreach (var childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }
    }
}
