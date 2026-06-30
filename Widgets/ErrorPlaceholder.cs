using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FoodEnterpriseIMS.Widgets
{
    /// <summary>
    /// 错误页面占位Label，对应Python ErrorPlaceholder
    /// </summary>
    public class ErrorPlaceholder : Label
    {
        public ErrorPlaceholder(string msg)
        {
            Content = msg;
            // 居中对齐
            HorizontalContentAlignment = HorizontalAlignment.Center;
            VerticalContentAlignment = VerticalAlignment.Center;
            // 内边距
            Padding = new Thickness(20);
            // 错误样式
            Foreground = Brushes.Red;
            FontSize = 14;
        }

        /// <summary>
        /// 原Python中switch_to_tab空方法，C#兼容空实现
        /// </summary>
        public void SwitchToTab(object arg)
        {
            // 无业务逻辑，仅兼容原代码接口
        }
    }
}