using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// C# 页面模块选择窗口
    /// </summary>
    public partial class PageModulePickerWindow : Window
    {
        private readonly ObservableCollection<PageModuleOption> _modules = new();
        private readonly ICollectionView _moduleView;

        public PageModuleOption? SelectedModule { get; private set; }

        public PageModulePickerWindow(IEnumerable<PageModuleOption> modules)
        {
            InitializeComponent();

            foreach (var module in modules.OrderBy(x => x.ComponentPath, StringComparer.OrdinalIgnoreCase))
            {
                _modules.Add(module);
            }

            _moduleView = CollectionViewSource.GetDefaultView(_modules);
            _moduleView.Filter = FilterModule;
            ModuleGrid.ItemsSource = _moduleView;
        }

        private bool FilterModule(object item)
        {
            if (item is not PageModuleOption option)
            {
                return false;
            }

            var keyword = FilterText.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return true;
            }

            return Contains(option.ComponentPath, keyword)
                   || Contains(option.CSharpClass, keyword)
                   || Contains(option.DisplayName, keyword);
        }

        private static bool Contains(string? text, string keyword)
        {
            return (text ?? string.Empty).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void FilterText_TextChanged(object sender, TextChangedEventArgs e)
        {
            _moduleView.Refresh();
        }

        private void ModuleGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CommitSelection();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            CommitSelection();
        }

        private void CommitSelection()
        {
            if (ModuleGrid.SelectedItem is not PageModuleOption selected)
            {
                MessageBox.Show("请选择一个窗体", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectedModule = selected;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class PageModuleOption
    {
        public string ComponentPath { get; set; } = string.Empty;
        public string CSharpClass { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}