using System;
using System.Windows;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 质量监督编辑窗口
    /// </summary>
    public partial class QualitySupervisionEditWindow : Window
    {
        /// <summary>
        /// 关闭请求事件，由主窗口处理
        /// </summary>
        public event EventHandler? CloseRequested;

        public bool Saved { get; private set; }

        private QualitySupervision _record;
        private readonly QualitySupervisionService _service;
        private string? _originalId;

        public QualitySupervisionEditWindow(QualitySupervision? record = null)
        {
            InitializeComponent();
            _service = new QualitySupervisionService();
            _record = record ?? new QualitySupervision
            {
                DiscoveryDate = DateTime.Today
            };
            _originalId = !string.IsNullOrWhiteSpace(_record.SupervisionId) ? _record.SupervisionId : null;
            DataContext = _record;
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            _record = new QualitySupervision
            {
                DiscoveryDate = DateTime.Today
            };
            _originalId = null;
            DataContext = _record;
            Saved = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_record.ProjectName))
            {
                MessageBox.Show("请填写项目名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(_record.SupervisionId) && _service.GetById(_record.SupervisionId) != null)
                {
                    _service.Update(_record);
                }
                else
                {
                    _service.Insert(_record);
                }
                Saved = true;
                MessageBox.Show("保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseRequested?.Invoke(this, EventArgs.Empty);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_originalId))
            {
                var restored = _service.GetById(_originalId);
                if (restored != null)
                {
                    _record = restored;
                    DataContext = _record;
                }
            }
            else
            {
                NewButton_Click(sender, e);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
            Close();
        }
    }
}
