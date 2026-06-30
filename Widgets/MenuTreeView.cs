using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FoodEnterpriseIMS.TreeCore;

namespace FoodEnterpriseIMS.Widgets
{
    /// <summary>
    /// 可拖拽的菜单树控件
    /// </summary>
    public class MenuTreeView : TreeView
    {
        public delegate bool DropHandlerDelegate(TreeViewItem source, TreeViewItem target, int dropPosition);

        public event DropHandlerDelegate DropHandler;

        #region Header 依赖属性
        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(MenuTreeView), new PropertyMetadata(string.Empty));
        #endregion

        public MenuTreeView()
        {
            AllowDrop = true;
            PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            PreviewMouseRightButtonDown += OnPreviewMouseRightButtonDown;
            Drop += OnDrop;
            InitializeContextMenu();
        }

        private TreeViewItem _draggedItem;
        private ContextMenu _contextMenu = null!;
        private bool _suppressItemClicked;

        /// <summary>
        /// 初始化右键菜单
        /// </summary>
        private void InitializeContextMenu()
        {
            _contextMenu = new ContextMenu();
            _contextMenu.Items.Add(CreateMenuItem("新增节点", (s, e) => RaiseNodeEvent(NodeAddRequestedEvent)));
            _contextMenu.Items.Add(CreateMenuItem("编辑节点", (s, e) => RaiseNodeEvent(NodeEditRequestedEvent)));
            _contextMenu.Items.Add(CreateMenuItem("删除节点", (s, e) => RaiseNodeEvent(NodeDeleteRequestedEvent)));
            _contextMenu.Items.Add(new Separator());
            _contextMenu.Items.Add(CreateMenuItem("展开节点", (s, e) => RaiseNodeEvent(NodeExpandRequestedEvent)));
            _contextMenu.Items.Add(CreateMenuItem("折叠节点", (s, e) => RaiseNodeEvent(NodeCollapseRequestedEvent)));
            ContextMenu = _contextMenu;
        }

        private static MenuItem CreateMenuItem(string header, RoutedEventHandler clickHandler)
        {
            var item = new MenuItem { Header = header };
            item.Click += clickHandler;
            return item;
        }

        /// <summary>
        /// 开始拖拽
        /// </summary>
        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _draggedItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
            if (_draggedItem == null) return;

            DragDrop.DoDragDrop(_draggedItem, _draggedItem, DragDropEffects.Move);
            _draggedItem = null;
        }

        /// <summary>
        /// 右键点击时先选中对应节点
        /// </summary>
        private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
            if (item == null) return;

            try
            {
                _suppressItemClicked = true;
                item.IsSelected = true;
                item.Focus();
            }
            finally
            {
                _suppressItemClicked = false;
            }
        }

        /// <summary>
        /// 拖拽放下
        /// </summary>
        private void OnDrop(object sender, DragEventArgs e)
        {
            if (_draggedItem == null || DropHandler == null) return;

            var targetItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
            if (targetItem == null) return;

            // 获取放下位置（简化版，实际需要计算位置）
            var dropPosition = 0;

            // 调用处理程序
            if (DropHandler(_draggedItem, targetItem, dropPosition))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// 查找父级TreeViewItem
        /// </summary>
        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>
        /// 根据当前选中的 TreeViewItem 构造轻量 TreeNode
        /// </summary>
        private TreeNode BuildNodeFromSelectedItem()
        {
            var item = SelectedItem as TreeViewItem;
            var node = new TreeNode();
            if (item != null)
            {
                node.Code = item.Tag?.ToString() ?? "";
                node.Title = item.Header?.ToString() ?? "";

                var parentItem = FindAncestor<TreeViewItem>(VisualTreeHelper.GetParent(item));
                node.ParentCode = parentItem?.Tag?.ToString();

                if (parentItem != null)
                {
                    node.SortOrder = parentItem.Items.IndexOf(item) + 1;
                }
                else
                {
                    node.SortOrder = Items.IndexOf(item) + 1;
                }
            }

            return node;
        }

        /// <summary>
        /// 触发自定义节点路由事件
        /// </summary>
        private void RaiseNodeEvent(RoutedEvent routedEvent)
        {
            var node = BuildNodeFromSelectedItem();
            var args = new TreeNodeRoutedEventArgs(routedEvent, node);
            RaiseEvent(args);
        }

        #region 事件封装
        // 封装ItemClicked事件
        public event RoutedEventHandler ItemClicked
        {
            add => AddHandler(ItemClickedEvent, value);
            remove => RemoveHandler(ItemClickedEvent, value);
        }

        public static readonly RoutedEvent ItemClickedEvent =
            EventManager.RegisterRoutedEvent("ItemClicked", RoutingStrategy.Bubble, 
                typeof(RoutedEventHandler), typeof(MenuTreeView));

        // 封装ItemDoubleClicked事件
        public event RoutedEventHandler ItemDoubleClicked
        {
            add => AddHandler(ItemDoubleClickedEvent, value);
            remove => RemoveHandler(ItemDoubleClickedEvent, value);
        }

        public static readonly RoutedEvent ItemDoubleClickedEvent =
            EventManager.RegisterRoutedEvent("ItemDoubleClicked", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(MenuTreeView));

        protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            base.OnSelectedItemChanged(e);
            if (!_suppressItemClicked)
            {
                RaiseEvent(new RoutedEventArgs(ItemClickedEvent, e.NewValue));
            }
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            RaiseEvent(new RoutedEventArgs(ItemDoubleClickedEvent, SelectedItem));
        }

        // 节点新增请求
        public event RoutedEventHandler NodeAddRequested
        {
            add => AddHandler(NodeAddRequestedEvent, value);
            remove => RemoveHandler(NodeAddRequestedEvent, value);
        }

        public static readonly RoutedEvent NodeAddRequestedEvent =
            EventManager.RegisterRoutedEvent("NodeAddRequested", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(MenuTreeView));

        // 节点编辑请求
        public event RoutedEventHandler NodeEditRequested
        {
            add => AddHandler(NodeEditRequestedEvent, value);
            remove => RemoveHandler(NodeEditRequestedEvent, value);
        }

        public static readonly RoutedEvent NodeEditRequestedEvent =
            EventManager.RegisterRoutedEvent("NodeEditRequested", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(MenuTreeView));

        // 节点删除请求
        public event RoutedEventHandler NodeDeleteRequested
        {
            add => AddHandler(NodeDeleteRequestedEvent, value);
            remove => RemoveHandler(NodeDeleteRequestedEvent, value);
        }

        public static readonly RoutedEvent NodeDeleteRequestedEvent =
            EventManager.RegisterRoutedEvent("NodeDeleteRequested", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(MenuTreeView));

        // 节点展开请求
        public event RoutedEventHandler NodeExpandRequested
        {
            add => AddHandler(NodeExpandRequestedEvent, value);
            remove => RemoveHandler(NodeExpandRequestedEvent, value);
        }

        public static readonly RoutedEvent NodeExpandRequestedEvent =
            EventManager.RegisterRoutedEvent("NodeExpandRequested", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(MenuTreeView));

        // 节点折叠请求
        public event RoutedEventHandler NodeCollapseRequested
        {
            add => AddHandler(NodeCollapseRequestedEvent, value);
            remove => RemoveHandler(NodeCollapseRequestedEvent, value);
        }

        public static readonly RoutedEvent NodeCollapseRequestedEvent =
            EventManager.RegisterRoutedEvent("NodeCollapseRequested", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(MenuTreeView));
        #endregion
    }
}
