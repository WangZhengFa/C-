using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            var (sys, tree) = ThemeConfigHelper.ReadAllConfigs(db);
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
            bool hideRoot = (bool)cfg["hide_root_branch"];

            // 通过 ItemContainerStyle 设置缩进、行高、分支线
            var itemStyle = new Style(typeof(TreeViewItem), tree.ItemContainerStyle);
            itemStyle.Setters.Add(new Setter(TreeViewItem.MinHeightProperty, (double)rowH));
            itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(2, 0, 0, 0)));
            tree.ItemContainerStyle = itemStyle;

            // WPF TreeView 不直接暴露 Indentation/RootLinesVisibility，
            // 后续可在 ControlTemplate 中通过 TreeViewItem 模板进一步定制。
            tree.UpdateLayout();
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
    }
}
