using System;
using System.Windows.Forms;

namespace FoodEnterpriseIMS.TreeCore
{
    public sealed class StandardTreeMenuActions
    {
        public Action? AddSibling { get; set; }
        public Action? AddChild { get; set; }
        public Action? EditNode { get; set; }
        public Action? DeleteNode { get; set; }

        public Action? CopySubtreeToRoot { get; set; }
        public Action? MoveNodeToTarget { get; set; }
        public Action? MoveUp { get; set; }
        public Action? MoveDown { get; set; }
        public Action? MoveTop { get; set; }
        public Action? MoveBottom { get; set; }

        public Action? AuditIntegrity { get; set; }
        public Action? NormalizeAllSiblingSort { get; set; }
        public Action? ExportJson { get; set; }
        public Action? ImportJson { get; set; }

        public Action? ExpandCurrent { get; set; }
        public Action? CollapseCurrent { get; set; }
        public Action? ExpandAll { get; set; }
        public Action? CollapseAll { get; set; }

        public Action? RefreshTree { get; set; }
    }

    /// <summary>
    /// WinForms 经典树样式 + 标准右键菜单（与 Python 版结构对齐）。
    /// </summary>
    public static class ClassicWinFormsTreeHelper
    {
        public static void ApplyClassicStyle(TreeView tree, int indent = 18, int itemHeight = 22)
        {
            if (tree == null)
            {
                return;
            }

            tree.BorderStyle = BorderStyle.None;
            tree.ShowLines = true;
            tree.ShowPlusMinus = true;
            tree.ShowRootLines = true;
            tree.HideSelection = false;
            tree.FullRowSelect = true;
            tree.HotTracking = false;
            tree.Indent = Math.Max(10, Math.Min(48, indent));
            tree.ItemHeight = Math.Max(18, Math.Min(48, itemHeight));
        }

        public static void AttachStandardContextMenu(TreeView tree, StandardTreeMenuActions actions)
        {
            if (tree == null || actions == null)
            {
                return;
            }

            var menu = new ContextMenuStrip();

            AddItem(menu.Items, "新增兄弟节点", actions.AddSibling);
            AddItem(menu.Items, "新增子节点", actions.AddChild);
            menu.Items.Add(new ToolStripSeparator());

            AddItem(menu.Items, "编辑节点", actions.EditNode);
            menu.Items.Add(new ToolStripSeparator());

            AddItem(menu.Items, "删除节点", actions.DeleteNode);

            var moveCopy = new ToolStripMenuItem("移动/复制到...");
            AddItem(moveCopy.DropDownItems, "复制子树到新根", actions.CopySubtreeToRoot);
            AddItem(moveCopy.DropDownItems, "移动节点（选择目标）", actions.MoveNodeToTarget);
            moveCopy.DropDownItems.Add(new ToolStripSeparator());
            AddItem(moveCopy.DropDownItems, "同级 → 上移", actions.MoveUp);
            AddItem(moveCopy.DropDownItems, "同级 → 下移", actions.MoveDown);
            AddItem(moveCopy.DropDownItems, "同级 → 置顶", actions.MoveTop);
            AddItem(moveCopy.DropDownItems, "同级 → 置底", actions.MoveBottom);
            menu.Items.Add(moveCopy);

            menu.Items.Add(new ToolStripSeparator());
            AddItem(menu.Items, "审计完整性", actions.AuditIntegrity);
            AddItem(menu.Items, "规范全部同级排序", actions.NormalizeAllSiblingSort);
            menu.Items.Add(new ToolStripSeparator());

            AddItem(menu.Items, "导出 JSON", actions.ExportJson);
            AddItem(menu.Items, "导入 JSON", actions.ImportJson);
            menu.Items.Add(new ToolStripSeparator());

            var expandCollapse = new ToolStripMenuItem("展开/折叠");
            AddItem(expandCollapse.DropDownItems, "展开当前节点", actions.ExpandCurrent);
            AddItem(expandCollapse.DropDownItems, "折叠当前节点", actions.CollapseCurrent);
            expandCollapse.DropDownItems.Add(new ToolStripSeparator());
            AddItem(expandCollapse.DropDownItems, "展开全部", actions.ExpandAll);
            AddItem(expandCollapse.DropDownItems, "折叠全部", actions.CollapseAll);
            menu.Items.Add(expandCollapse);

            menu.Items.Add(new ToolStripSeparator());
            AddItem(menu.Items, "刷新树", actions.RefreshTree);

            tree.ContextMenuStrip = menu;
        }

        private static void AddItem(ToolStripItemCollection items, string text, Action? action)
        {
            var item = new ToolStripMenuItem(text)
            {
                Enabled = action != null
            };

            if (action != null)
            {
                item.Click += (_, _) => action();
            }

            items.Add(item);
        }
    }
}
