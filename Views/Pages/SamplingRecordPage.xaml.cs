using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MySqlConnector;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;
using FoodEnterpriseIMS.TreeCore;
using 食品信息管理系统.Views.Dialogs;

namespace 食品信息管理系统.Views.Pages
{
    /// <summary>
    /// 取样记录页面
    /// </summary>
    public partial class SamplingRecordPage : Page
    {
        /// <summary>
        /// 关闭请求事件，由主窗口处理
        /// </summary>
        public event EventHandler? CloseRequested;

        private readonly SamplingRecordService _service;
        private ObservableCollection<SamplingRecord> _records;

        public SamplingRecordPage()
        {
            InitializeComponent();
            _service = new SamplingRecordService();
            _records = new ObservableCollection<SamplingRecord>();
            RecordGrid.ItemsSource = _records;
            LoadMaterialNodes();
            LoadRecords();
        }

        #region 加载数据
        /// <summary>
        /// 加载 material_nodes 树，仅加载 depth <= 2
        /// </summary>
        private void LoadMaterialNodes()
        {
            NodeTree.Items.Clear();
            try
            {
                var cfg = MysqlDbInitializer.LoadMysqlConfig();
                var connStr = $"server={cfg.Host};port={cfg.Port};user={cfg.User};password={cfg.Password};database={cfg.Database};charset=utf8mb4;Pooling=true;Max Pool Size=10;Min Pool Size=1";
                using var conn = new MySqlConnection(connStr);
                conn.Open();
                var repo = new TreeRepository(conn, "material_nodes");
                var nodes = repo.ListNodes(2);
                BuildTree(NodeTree.Items, nodes, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载物料节点失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void BuildTree(ItemCollection parent, List<Dictionary<string, object>> nodes, string? parentCode)
        {
            foreach (var node in nodes.Where(n => (n.GetValueOrDefault("parent_code") as string ?? string.Empty) == (parentCode ?? string.Empty)))
            {
                var code = node.GetValueOrDefault("code") as string ?? string.Empty;
                var title = node.GetValueOrDefault("title") as string ?? code;
                var item = new TreeViewItem { Header = title, Tag = code };
                BuildTree(item.Items, nodes, code);
                parent.Add(item);
            }
        }

        private void LoadRecords(string? nodeCode = null)
        {
            _records.Clear();
            foreach (var r in _service.ListByNodeCode(nodeCode))
            {
                _records.Add(r);
            }
        }

        private void NodeTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (NodeTree.SelectedItem is TreeViewItem item)
            {
                LoadRecords(item.Tag?.ToString());
            }
        }
        #endregion

        #region 工具栏事件
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            OpenEditWindow(null);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is SamplingRecord record)
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
            if (RecordGrid.SelectedItem is not SamplingRecord record)
            {
                MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show("确定删除选中的取样记录？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                _service.Delete(record.Id);
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
                Title = "导入取样记录"
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
                Title = "导出取样记录",
                FileName = $"取样记录_{DateTime.Now:yyyyMMddHHmmss}.csv"
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
        #endregion

        private void OpenEditWindow(SamplingRecord? record)
        {
            var window = new SamplingRecordEditWindow(record)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
            LoadRecords();
        }
    }
}
