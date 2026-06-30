#if WINFORMS
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FoodEnterpriseIMS.TreeCore
{
    /// <summary>
    /// WinForms TreeView 适配层，复刻 Python QtTreeAdapter UI 逻辑
    /// 需要在 WinForms 项目中定义 WINFORMS 条件编译符号
    /// </summary>
    public class TreeViewAdapter
    {
        private readonly TreeView _treeView;
        private readonly TreeOperations _ops;
        private readonly Timer _refreshTimer;
        private bool _refreshInProgress;
        private int _minWidth = 120;
        private int _maxWidth = 420;
        private int _initWidth = 120;

        public TreeView TreeView => _treeView;
        public TreeOperations Ops => _ops;

        public TreeViewAdapter(TreeView treeView, TreeOperations ops)
        {
            _treeView = treeView ?? throw new ArgumentNullException(nameof(treeView));
            _ops = ops ?? throw new ArgumentNullException(nameof(ops));
            _refreshTimer = new Timer { Interval = 50, Enabled = false };
            _refreshTimer.Tick += DoRefresh;
            _treeView.AfterExpand += (s, e) => ScheduleRefresh();
            _treeView.AfterCollapse += (s, e) => ScheduleRefresh();
        }

        /// <summary> 标准样式与行为初始化 </summary>
        public void ApplyStandardBehavior(int initialW = 120, int minW = 120, int maxW = 320, bool autoWidth = true)
        {
            _treeView.ShowRootLines = true;
            _treeView.HotTracking = true;
            _treeView.Indent = 16;
            _treeView.Scrollable = true;
            _treeView.HorizontalScrollbar = false;
            _initWidth = initialW;
            _minWidth = minW;
            _maxWidth = maxW;
            if (autoWidth) EnableAutoWidth();
            ScheduleRefresh();
        }

        /// <summary> 防抖延迟刷新 </summary>
        public void ScheduleRefresh(int delayMs = 50)
        {
            _refreshTimer.Interval = delayMs;
            _refreshTimer.Stop();
            _refreshTimer.Start();
        }

        private void DoRefresh(object? sender, EventArgs e)
        {
            if (_refreshInProgress || _treeView.IsDisposed) return;
            _refreshTimer.Stop();
            _refreshInProgress = true;
            try
            {
                SyncTreeAutoWidth();
            }
            finally
            {
                _refreshInProgress = false;
            }
        }

        /// <summary> 自动宽度计算适配 </summary>
        public void EnableAutoWidth()
        {
            SyncTreeAutoWidth();
        }

        private void SyncTreeAutoWidth()
        {
            int maxTextW = 0;
            Font f = _treeView.Font;
            using Graphics g = Graphics.FromHwnd(_treeView.Handle);

            void CalcNodeWidth(System.Windows.Forms.TreeNode node)
            {
                int w = TextRenderer.MeasureText(g, node.Text, f).Width + 40;
                if (w > maxTextW) maxTextW = w;
                foreach (System.Windows.Forms.TreeNode child in node.Nodes)
                    CalcNodeWidth(child);
            }

            foreach (System.Windows.Forms.TreeNode root in _treeView.Nodes)
                CalcNodeWidth(root);

            int target = Math.Clamp(maxTextW, _minWidth, _maxWidth);
            _treeView.Width = target;
        }

        /// <summary> 从编码选中节点并滚动到可视区域 </summary>
        public bool SelectNodeByCode(string code)
        {
            System.Windows.Forms.TreeNode? Find(System.Windows.Forms.TreeNodeCollection nodes)
            {
                foreach (System.Windows.Forms.TreeNode n in nodes)
                {
                    if (n.Tag?.ToString() == code) return n;
                    var sub = Find(n.Nodes);
                    if (sub != null) return sub;
                }
                return null;
            }

            var target = Find(_treeView.Nodes);
            if (target == null) return false;
            _treeView.SelectedNode = target;
            target.EnsureVisible();
            return true;
        }

        /// <summary> 重建 TreeView 界面 </summary>
        public void RebuildUi(int expandDepth = 1)
        {
            _treeView.BeginUpdate();
            try
            {
                _treeView.Nodes.Clear();
                var roots = _ops.BuildTree();
                foreach (var root in roots.OrderBy(x => x.SortOrder))
                    RecursiveAdd(null, root, expandDepth);
            }
            finally
            {
                _treeView.EndUpdate();
            }
            ScheduleRefresh();
        }

        private void RecursiveAdd(System.Windows.Forms.TreeNode? parentUi, TreeNode modelNode, int expandDepth)
        {
            var uiNode = new System.Windows.Forms.TreeNode(modelNode.Title)
            {
                Tag = modelNode.Code
            };
            int depth = TreeCodeHelper.Depth(modelNode.Code);
            if (depth <= expandDepth) uiNode.Expand();

            if (parentUi == null)
                _treeView.Nodes.Add(uiNode);
            else
                parentUi.Nodes.Add(uiNode);

            foreach (var childModel in modelNode.Children.OrderBy(x => x.SortOrder))
                RecursiveAdd(uiNode, childModel, expandDepth);
        }

        /// <summary> 绑定拖拽移动 </summary>
        public void EnableDragDrop()
        {
            _treeView.AllowDrop = true;
            _treeView.ItemDrag += (s, e) => _treeView.DoDragDrop(e.Item!, DragDropEffects.Move);
            _treeView.DragEnter += (s, e) => e.Effect = DragDropEffects.Move;
            _treeView.DragDrop += TreeDragDrop;
        }

        private void TreeDragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(typeof(System.Windows.Forms.TreeNode)) is not System.Windows.Forms.TreeNode sourceUi) return;
            string? sourceCode = sourceUi.Tag?.ToString();
            if (string.IsNullOrEmpty(sourceCode)) return;

            Point pt = _treeView.PointToClient(new Point(e.X, e.Y));
            var targetUi = _treeView.GetNodeAt(pt);
            string? targetParentCode = targetUi?.Tag?.ToString();

            // 执行后台移动逻辑
            _ops.MoveNode(sourceCode, targetParentCode, regenerateCodes: false);
            RebuildUi();
        }
    }
}
#endif
