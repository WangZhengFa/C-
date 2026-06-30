using System;
using System.Windows;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 取样记录编辑窗口
    /// </summary>
    public partial class SamplingRecordEditWindow : Window
    {
        /// <summary>
        /// 关闭请求事件，由主窗口处理
        /// </summary>
        public event EventHandler? CloseRequested;

        public bool Saved { get; private set; }

        private SamplingRecord _record;
        private readonly SamplingRecordService _service;
        private long? _originalId;

        public SamplingRecordEditWindow(SamplingRecord? record = null)
        {
            InitializeComponent();
            _service = new SamplingRecordService();
            _record = record ?? new SamplingRecord
            {
                SamplingDate = DateTime.Today,
                InspectionDate = DateTime.Today
            };
            _originalId = _record.Id > 0 ? _record.Id : null;
            DataContext = _record;
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            _record = new SamplingRecord
            {
                SamplingDate = DateTime.Today,
                InspectionDate = DateTime.Today
            };
            _originalId = null;
            DataContext = _record;
            Saved = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_record.SampleName))
            {
                MessageBox.Show("请填写检品名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(_record.NodeCode))
            {
                MessageBox.Show("请填写节点编号", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_record.Id > 0)
                {
                    _service.Update(_record);
                }
                else
                {
                    var newId = _service.Insert(_record);
                    _record.Id = newId;
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
            if (_originalId.HasValue)
            {
                var restored = _service.GetById(_originalId.Value);
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
