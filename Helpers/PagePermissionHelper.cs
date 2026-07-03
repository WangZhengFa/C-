using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FoodEnterpriseIMS.Services;

namespace FoodEnterpriseIMS.Helpers
{
    public static class PagePermissionHelper
    {
        private static readonly Dictionary<string, string> ContentToButtonKey = new(StringComparer.OrdinalIgnoreCase)
        {
            ["新增"] = "add_btn",
            ["编辑"] = "edit_btn",
            ["删除"] = "delete_btn",
            ["导入"] = "import_btn",
            ["导出"] = "export_btn",
            ["设置"] = "settings_btn",
            ["关闭"] = "close_btn",
            ["刷新"] = "refresh_btn",
            ["保存"] = "save_btn",
            ["取消"] = "cancel_btn"
        };

        public static void ApplyButtonPermissions(FrameworkElement root, string menuKey, int roleId, DatabaseManager db)
        {
            if (root == null || db == null || string.IsNullOrWhiteSpace(menuKey))
            {
                return;
            }

            if (roleId == 1)
            {
                return;
            }

            var definedButtonPermissions = db.GetDefinedButtonPermissionKeys(menuKey);
            if (definedButtonPermissions.Count == 0)
            {
                // 菜单下未定义任何按钮权限时，不做限制，避免误伤现有页面。
                return;
            }

            var rolePermissions = new HashSet<string>(db.GetRolePermissions(roleId), StringComparer.OrdinalIgnoreCase);
            var definedSet = new HashSet<string>(definedButtonPermissions, StringComparer.OrdinalIgnoreCase);

            foreach (var button in FindVisualChildren<Button>(root))
            {
                var buttonKey = ResolveButtonKey(button);
                if (string.IsNullOrWhiteSpace(buttonKey))
                {
                    continue;
                }

                var permissionKey = $"{menuKey}:{buttonKey}";
                if (!definedSet.Contains(permissionKey))
                {
                    continue;
                }

                var allowed = rolePermissions.Contains(permissionKey);
                button.IsEnabled = allowed;
                button.Visibility = allowed ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static string ResolveButtonKey(Button button)
        {
            var tagKey = button.Tag?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(tagKey))
            {
                return NormalizeButtonKey(tagKey);
            }

            var content = button.Content?.ToString()?.Trim() ?? string.Empty;
            return ContentToButtonKey.TryGetValue(content, out var mapped) ? mapped : string.Empty;
        }

        private static string NormalizeButtonKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var value = key.Trim();
            if (value.StartsWith("btn_", StringComparison.OrdinalIgnoreCase) && value.Length > 4)
            {
                return value[4..] + "_btn";
            }
            if (value.StartsWith("button_", StringComparison.OrdinalIgnoreCase) && value.Length > 7)
            {
                return value[7..] + "_btn";
            }
            if (value.EndsWith("_button", StringComparison.OrdinalIgnoreCase) && value.Length > 7)
            {
                return value[..^7] + "_btn";
            }
            return value;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) yield break;

            var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
                if (child is T matched)
                {
                    yield return matched;
                }

                foreach (var nested in FindVisualChildren<T>(child))
                {
                    yield return nested;
                }
            }
        }
    }
}
