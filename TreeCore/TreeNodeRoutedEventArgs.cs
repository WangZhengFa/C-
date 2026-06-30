using System.Windows;

namespace FoodEnterpriseIMS.TreeCore
{
    /// <summary>
    /// 携带树节点的路由事件参数
    /// </summary>
    public class TreeNodeRoutedEventArgs : RoutedEventArgs
    {
        public TreeNode Node { get; }

        public TreeNodeRoutedEventArgs(RoutedEvent routedEvent, TreeNode node)
            : base(routedEvent, node)
        {
            Node = node;
        }
    }
}
