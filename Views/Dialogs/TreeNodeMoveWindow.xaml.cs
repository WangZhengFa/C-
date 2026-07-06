using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FoodEnterpriseIMS.TreeCore;

namespace 食品信息管理系统.Views.Dialogs
{
    public partial class TreeNodeMoveWindow : Window
    {
        private readonly TreeNode _sourceNode;
        private readonly List<TreeNode> _availableRoots;
        private readonly Dictionary<string?, TreeNode> _nodeLookup = new();

        public string? SelectedParentCode { get; private set; }
        public bool RegenerateCodes => RegenerateCodesCheckBox.IsChecked == true;
        public int? TargetIndex { get; private set; }

        public TreeNodeMoveWindow(IEnumerable<TreeNode> roots, TreeNode sourceNode)
        {
            InitializeComponent();
            _sourceNode = sourceNode ?? throw new ArgumentNullException(nameof(sourceNode));
            _availableRoots = FilterRoots(roots, _sourceNode.Code);

            CurrentTitleText.Text = _sourceNode.Title;
            CurrentCodeText.Text = _sourceNode.Code;

            BuildLookup(_availableRoots);
            BuildTreeView();

            RegenerateCodesCheckBox.IsChecked = true;
            SelectRootNode();
            UpdatePositionHint();
        }

        private static List<TreeNode> FilterRoots(IEnumerable<TreeNode> roots, string sourceCode)
        {
            var filtered = new List<TreeNode>();
            foreach (var root in roots.OrderBy(x => x.SortOrder).ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase))
            {
                if (string.Equals(root.Code, sourceCode, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                filtered.Add(CloneWithoutSource(root, sourceCode));
            }

            return filtered;
        }

        private static TreeNode CloneWithoutSource(TreeNode node, string sourceCode)
        {
            var copy = new TreeNode
            {
                Code = node.Code,
                Title = node.Title,
                Payload = new Dictionary<string, object>(node.Payload),
                SortOrder = node.SortOrder,
                ParentCode = node.ParentCode
            };

            foreach (var child in node.Children.OrderBy(x => x.SortOrder).ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase))
            {
                if (string.Equals(child.Code, sourceCode, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                copy.Children.Add(CloneWithoutSource(child, sourceCode));
            }

            return copy;
        }

        private void BuildLookup(IEnumerable<TreeNode> roots)
        {
            _nodeLookup[null] = new TreeNode
            {
                Code = string.Empty,
                Title = "根节点",
                Children = roots.ToList()
            };

            foreach (var root in roots)
            {
                IndexNode(root);
            }
        }

        private void IndexNode(TreeNode node)
        {
            _nodeLookup[node.Code] = node;
            foreach (var child in node.Children)
            {
                IndexNode(child);
            }
        }

        private void BuildTreeView()
        {
            ParentTreeView.Items.Clear();
            var rootItem = CreateTreeViewItem(null, "根节点", _availableRoots);
            rootItem.IsExpanded = true;
            ParentTreeView.Items.Add(rootItem);
        }

        private static TreeViewItem CreateTreeViewItem(string? code, string title, IEnumerable<TreeNode> children)
        {
            var item = new TreeViewItem
            {
                Header = title,
                Tag = code
            };

            foreach (var child in children.OrderBy(x => x.SortOrder).ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase))
            {
                var childItem = CreateTreeViewItem(child.Code, FormatNodeTitle(child), child.Children);
                item.Items.Add(childItem);
            }

            return item;
        }

        private static string FormatNodeTitle(TreeNode node)
        {
            return string.IsNullOrWhiteSpace(node.Code) ? node.Title : $"{node.Title} [{node.Code}]";
        }

        private void SelectRootNode()
        {
            if (ParentTreeView.Items.Count > 0 && ParentTreeView.Items[0] is TreeViewItem root)
            {
                root.IsSelected = true;
            }
        }

        private void ParentTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedParentCode = (ParentTreeView.SelectedItem as TreeViewItem)?.Tag as string;
            UpdatePositionHint();
        }

        private void UpdatePositionHint()
        {
            var count = GetSiblingCount(SelectedParentCode);
            PositionHintText.Text = count <= 0
                ? "当前父节点没有可见同级节点，留空表示追加到最后。"
                : $"有效范围：1 ~ {count + 1}，留空表示追加到最后。";

            SelectionHintText.Text = string.IsNullOrWhiteSpace(SelectedParentCode)
                ? "当前选择：顶级(根节点)"
                : $"当前选择：{GetNodeDisplayText(SelectedParentCode)}";

            if (string.IsNullOrWhiteSpace(TargetIndexText.Text))
            {
                return;
            }

            if (int.TryParse(TargetIndexText.Text, out var index))
            {
                var max = count + 1;
                if (max > 0)
                {
                    var clamped = Math.Clamp(index, 1, max);
                    if (clamped != index)
                    {
                        TargetIndexText.Text = clamped.ToString();
                    }
                }
            }
        }

        private int GetSiblingCount(string? parentCode)
        {
            if (!_nodeLookup.TryGetValue(parentCode, out var parent))
            {
                return 0;
            }

            return parent.Children.Count;
        }

        private string GetNodeDisplayText(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return "根节点";
            }

            return _nodeLookup.TryGetValue(code, out var node)
                ? $"{node.Title} [{node.Code}]"
                : code;
        }

        private void TargetIndexText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var count = GetSiblingCount(SelectedParentCode);
            if (int.TryParse(TargetIndexText.Text, out var index))
            {
                var max = count + 1;
                if (max > 0)
                {
                    index = Math.Clamp(index, 1, max);
                    TargetIndex = index - 1;
                }
            }
            else
            {
                TargetIndex = null;
            }

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}