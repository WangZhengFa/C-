using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FoodEnterpriseIMS.Helpers
{
    /// <summary>
    /// 对话框回车焦点导航器
    /// </summary>
    public static class DialogEnterFocusNavigator
    {
        private static readonly List<string> _titleKeywords = new List<string> { "新增", "编辑" };
        private static readonly List<string> _classKeywords = new List<string> { "EditDialog", "EditorDialog", "AddDialog", "RecordDialog" };
        private static readonly HashSet<IntPtr> _preparedDialogs = new HashSet<IntPtr>();

        /// <summary>
        /// 初始化回车导航
        /// </summary>
        public static void Init(Application app)
        {
            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
            EventManager.RegisterClassHandler(typeof(UIElement), UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown));
        }

        /// <summary>
        /// 窗口加载事件
        /// </summary>
        private static void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            var window = sender as Window;
            if (window == null || !IsEditLikeDialog(window)) return;

            PrepareDialog(window);
        }

        /// <summary>
        /// 按键事件
        /// </summary>
        private static void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter && e.Key != Key.Return) return;

            var element = sender as UIElement;
            var dialog = FindParentDialog(element);
            if (dialog == null || !IsEditLikeDialog(dialog)) return;

            PrepareDialog(dialog);
            
            // 文本框/富文本框保留原有行为
            if (element is TextBox tb && tb.AcceptsReturn) return;
            if (element is RichTextBox) return;

            // 下拉框展开时保留原有行为
            if (element is ComboBox cb && cb.IsDropDownOpen) return;

            // 焦点跳转到下一个输入控件
            if (FocusNextInput(dialog))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// 查找父级对话框
        /// </summary>
        private static Window FindParentDialog(UIElement element)
        {
            if (element == null) return null;

            var parent = VisualTreeHelper.GetParent(element);
            while (parent != null)
            {
                if (parent is Window window)
                {
                    return window;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        /// <summary>
        /// 判断是否是编辑类对话框
        /// </summary>
        private static bool IsEditLikeDialog(Window window)
        {
            if (window == null) return false;

            var title = window.Title ?? "";
            var className = window.GetType().Name;

            return _titleKeywords.Exists(k => title.Contains(k)) ||
                   _classKeywords.Exists(k => className.Contains(k));
        }

        /// <summary>
        /// 准备对话框（取消按钮默认行为）
        /// </summary>
        private static void PrepareDialog(Window dialog)
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(dialog).Handle;
            if (_preparedDialogs.Contains(handle)) return;

            _preparedDialogs.Add(handle);

            // 遍历所有按钮，取消默认按钮行为
            foreach (var btn in FindVisualChildren<Button>(dialog))
            {
                btn.IsDefault = false;
                btn.IsCancel = false;
            }
        }

        /// <summary>
        /// 焦点跳转到下一个输入控件
        /// </summary>
        private static bool FocusNextInput(Window dialog)
        {
            var controls = FindVisualChildren<UIElement>(dialog)
                .Where(c => IsSupportedInputControl(c))
                .ToList();

            if (controls.Count == 0) return false;

            var currentIndex = controls.IndexOf(FocusManager.GetFocusedElement(dialog) as UIElement);
            var nextIndex = (currentIndex + 1) % controls.Count;

            controls[nextIndex].Focus();
            return true;
        }

        /// <summary>
        /// 判断是否是支持的输入控件
        /// </summary>
        private static bool IsSupportedInputControl(UIElement element)
        {
            return element is TextBox || 
                   element is ComboBox || 
                   element is DatePicker ||
                   element is PasswordBox;
        }

        /// <summary>
        /// 查找可视化子元素
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
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