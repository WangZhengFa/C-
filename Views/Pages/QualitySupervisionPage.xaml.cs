using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;
using 食品信息管理系统.Views.Dialogs;

namespace 食品信息管理系统.Views.Pages
{
    /// <summary>
    /// 质量监督页面
    /// </summary>
    public partial class QualitySupervisionPage : Page
    {
        /// <summary>
        /// 关闭请求事件，由主窗口处理
        /// </summary>
        public event EventHandler? CloseRequested;

        private readonly QualitySupervisionService _service;
        private ObservableCollection<QualitySupervision> _records;

        public QualitySupervisionPage()
        {
            InitializeComponent();
            _service = new QualitySupervisionService();
            _records = new ObservableCollection<QualitySupervision>();
            RecordGrid.ItemsSource = _records;
            LoadRecords();
        }

        private void LoadRecords()
        {
            _records.Clear();
            foreach (var r in _service.ListAll())
            {
                _records.Add(r);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            OpenEditWindow(null);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is QualitySupervision record)
            {
                OpenEditWindow(record);
            }
            else
            {
                MessageBox.Show("请选择一条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not QualitySupervision record)
            {
                MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show("确定删除选中的质量监督记录？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                _service.Delete(record.SupervisionId);
                _records.Remove(record);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
                Title = "导入质量监督记录"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var (success, fail) = _service.ImportFromCsv(dialog.FileName);
                MessageBox.Show($"导入完成：成功 {success} 条，失败 {fail} 条", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv",
                Title = "导出质量监督记录",
                FileName = $"质量监督_{DateTime.Now:yyyyMMddHHmmss}.csv"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                _service.ExportToCsv(dialog.FileName, _records);
                MessageBox.Show("导出成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("设置功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OpenEditWindow(QualitySupervision? record)
        {
            var window = new QualitySupervisionEditWindow(record)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
            LoadRecords();
        }
    }
}
