using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FoodEnterpriseIMS.Services;

namespace FoodEnterpriseIMS.Themes
{
    /// <summary>
    /// 树控件样式辅助，对应 theme_manager 树委托、行高、经典系统样式
    /// </summary>
    public static class TreeStyleHelper
    {
        /// <summary>
        /// 应用全局树配置
        /// </summary>
        public static Dictionary<string, object> ReadTreeGlobalConfig(DatabaseManager db)
        {
            int indent = ThemeConfigHelper.CfgInt(db, "Settings", "tree_indent", 18);
            int rowH = ThemeConfigHelper.CfgInt(db, "Settings", "tree_row_height", 26);
            int indSz = ThemeConfigHelper.CfgInt(db, "Settings", "indicator_size", 12);
            bool hideRoot = ThemeConfigHelper.CfgBool(db, "Settings", "hide_root_branch", false);
            bool useSysStyle = ThemeConfigHelper.CfgBool(db, "Settings", "tree_classic_use_system", true);
            int expandLv = ThemeConfigHelper.CfgInt(db, "Settings", "tree_expand_level", 2);

            return new()
            {
                ["theme"] = ThemeConfigHelper.CfgGet(db, "Settings", "theme_preference", "light"),
                ["indent"] = indent,
                ["row_height"] = rowH,
                ["indicator_size"] = indSz,
                ["hide_root_branch"] = hideRoot,
                ["classic_use_system"] = useSysStyle,
                ["expand_level"] = expandLv
            };
        }

        /// <summary>
        /// 统一应用树样式到 TreeView
        /// </summary>
        public static void ApplyGlobalTreeStyle(TreeView tree, DatabaseManager db)
        {
            if (tree == null) return;

            var cfg = ReadTreeGlobalConfig(db);
            int indent = (int)cfg["indent"];
            int rowH = (int)cfg["row_height"];
            int indicatorSize = (int)cfg["indicator_size"];
            bool hideRootBranch = (bool)cfg["hide_root_branch"];
            bool useSysStyle = (bool)cfg["classic_use_system"];

            // 继续压缩缩进，让窄侧栏下层级文本尽量完整显示。
            var leftPadding = Math.Max(0, indent / 6.0);

            // 通过 ItemContainerStyle 设置缩进、行高、分支线
            var itemStyle = new Style(typeof(TreeViewItem), tree.ItemContainerStyle);
            itemStyle.Setters.Add(new Setter(TreeViewItem.MinHeightProperty, (double)rowH));
            itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(leftPadding, 0, 0, 0)));
            tree.ItemContainerStyle = itemStyle;

            if (useSysStyle)
            {
                tree.BorderThickness = new Thickness(0);
                tree.Background = SystemColors.WindowBrush;
            }

            tree.UpdateLayout();
            ApplyRuntimeVisualOverrides(tree, indicatorSize, hideRootBranch);
        }

        /// <summary>
        /// 按层级展开/折叠树节点
        /// </summary>
        public static void ExpandTreeLevel(TreeView tree, int level)
        {
            if (tree == null) return;

            if (level <= 0)
            {
                SetAllItemsExpanded(tree, false);
            }
            else if (level == 1)
            {
                foreach (var item in tree.Items)
                {
                    if (GetTreeViewItem(tree, item) is TreeViewItem tvi)
                    {
                        tvi.IsExpanded = true;
                        SetChildrenExpanded(tvi, false);
                    }
                }
            }
            else
            {
                SetAllItemsExpanded(tree, true);
            }
        }

        private static void SetAllItemsExpanded(ItemsControl parent, bool expanded)
        {
            if (parent == null) return;

            foreach (var item in parent.Items)
            {
                if (GetTreeViewItem(parent, item) is TreeViewItem tvi)
                {
                    tvi.IsExpanded = expanded;
                    SetChildrenExpanded(tvi, expanded);
                }
            }
        }

        private static void SetChildrenExpanded(TreeViewItem parent, bool expanded)
        {
            if (parent == null) return;

            foreach (var item in parent.Items)
            {
                if (GetTreeViewItem(parent, item) is TreeViewItem tvi)
                {
                    tvi.IsExpanded = expanded;
                    SetChildrenExpanded(tvi, expanded);
                }
            }
        }

        private static TreeViewItem? GetTreeViewItem(ItemsControl parent, object item)
        {
            if (parent.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                return null;
            return parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem
                ?? parent.ItemContainerGenerator.ContainerFromIndex(parent.Items.IndexOf(item)) as TreeViewItem;
        }

        private static void ApplyRuntimeVisualOverrides(TreeView tree, int indicatorSize, bool hideRootBranch)
        {
            var safeIndicatorSize = Math.Max(8, Math.Min(40, indicatorSize));

            foreach (var item in tree.Items)
            {
                if (GetTreeViewItem(tree, item) is TreeViewItem tvi)
                {
                    ApplyRuntimeVisualOverrides(tvi, 0, safeIndicatorSize, hideRootBranch);
                }
            }
        }

        private static void ApplyRuntimeVisualOverrides(TreeViewItem item, int depth, int indicatorSize, bool hideRootBranch)
        {
            item.ApplyTemplate();

            // TreeViewItem 模板中的 Expander 内部是固定 16x16，
            // 通过缩放保持模板兼容同时支持运行时尺寸配置。
            if (item.Template.FindName("Expander", item) is FrameworkElement expander)
            {
                var scale = indicatorSize / 16.0;
                expander.LayoutTransform = new ScaleTransform(scale, scale);
            }

            if (item.Template.FindName("ItemsHost", item) is FrameworkElement itemsHost)
            {
                var childIndent = Math.Max(6.0, indicatorSize * 0.6);
                itemsHost.Margin = new Thickness(childIndent, 0, 0, 0);
            }

            var hideCurrentBranch = hideRootBranch && depth == 0;
            if (item.Template.FindName("HorLn", item) is Rectangle horLine)
            {
                horLine.Visibility = hideCurrentBranch ? Visibility.Collapsed : Visibility.Visible;
            }

            if (item.Template.FindName("VerLn", item) is Rectangle verLine)
            {
                verLine.Visibility = hideCurrentBranch ? Visibility.Collapsed : Visibility.Visible;
            }

            item.UpdateLayout();
            foreach (var child in item.Items)
            {
                if (GetTreeViewItem(item, child) is TreeViewItem childItem)
                {
                    ApplyRuntimeVisualOverrides(childItem, depth + 1, indicatorSize, hideRootBranch);
                }
            }
        }
    }
}
