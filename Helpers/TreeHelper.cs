using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using FoodEnterpriseIMS.Widgets;

namespace FoodEnterpriseIMS.Helpers
{
    /// <summary>
    /// 树控件相关辅助：拖拽、菜单树等
    /// </summary>
    public static class TreeHelper
    {
        /// <summary>
        /// 构建菜单树
        /// </summary>
        public static void BuildMenuTree(MenuTreeView tree, List<Dictionary<string, object>> menuList, int roleId)
        {
            tree.Items.Clear();
            if (menuList == null || menuList.Count == 0) return;

            // 按 parent_key 分组，空字符串/NULL 视为根节点
            var groups = menuList
                .Where(m => m != null)
                .GroupBy(m => GetString(m, "parent_key"))
                .ToDictionary(g => g.Key, g => g.OrderBy(item => GetInt(item, "sort_order")).ToList());

            if (!groups.TryGetValue("", out var roots))
            {
                roots = menuList.Where(m => string.IsNullOrEmpty(GetString(m, "parent_key"))).ToList();
            }

            foreach (var menu in roots)
            {
                var node = CreateTreeNode(menu, groups);
                if (node != null)
                {
                    tree.Items.Add(node);
                    // 展开根节点
                    node.IsExpanded = true;
                }
            }
        }

        private static TreeViewItem CreateTreeNode(Dictionary<string, object> menu,
            Dictionary<string, List<Dictionary<string, object>>> groups)
        {
            var key = GetString(menu, "menu_key");
            var title = GetString(menu, "title");
            if (string.IsNullOrEmpty(key) && string.IsNullOrEmpty(title)) return null;

            var item = new TreeViewItem
            {
                Header = string.IsNullOrEmpty(title) ? key : title,
                Tag = key,
                IsExpanded = true  // 默认展开所有节点
            };

            if (groups.TryGetValue(key, out var children))
            {
                foreach (var child in children)
                {
                    var childNode = CreateTreeNode(child, groups);
                    if (childNode != null)
                        item.Items.Add(childNode);
                }
            }

            return item;
        }

        private static string GetString(Dictionary<string, object> dict, string key)
        {
            if (dict == null) return string.Empty;
            return dict.TryGetValue(key, out var value) && value != null ? value.ToString() : string.Empty;
        }

        private static int GetInt(Dictionary<string, object> dict, string key)
        {
            if (dict == null) return 0;
            var s = GetString(dict, key);
            return int.TryParse(s, out var v) ? v : 0;
        }
    }
}
