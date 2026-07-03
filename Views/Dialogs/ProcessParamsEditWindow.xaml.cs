using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 工序参数编辑窗口
    /// </summary>
    public partial class ProcessParamsEditWindow : Window
    {
        public ProcessParamsRecord Value { get; private set; }

        public ProcessParamsEditWindow(ProcessParamsRecord? source, IEnumerable<ProcessParamsRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new ProcessParamsRecord { IsDisabled = false } : Clone(source);
            BindValue();
        }

        private void BindValue()
        {
            ProcessStepIdText.Text = Value.ProcessStepId;
            ProductIdText.Text = Value.ProductId;
            ProcessNameText.Text = Value.ProcessName;
            IsDisabledCheck.IsChecked = Value.IsDisabled;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var processStepId = ProcessStepIdText.Text.Trim();
            var processName = ProcessNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(processStepId))
            {
                MessageBox.Show(this, "工序步骤ID不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(processName))
            {
                MessageBox.Show(this, "工序名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.ProcessStepId = processStepId;
            Value.ProductId = ProductIdText.Text.Trim();
            Value.ProcessName = processName;
            Value.IsDisabled = IsDisabledCheck.IsChecked == true;
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static ProcessParamsRecord Clone(ProcessParamsRecord source)
        {
            return new ProcessParamsRecord
            {
                Id = source.Id,
                ProcessStepId = source.ProcessStepId,
                ProductId = source.ProductId,
                ProcessName = source.ProcessName,
                IsDisabled = source.IsDisabled,
                Remark = source.Remark
            };
        }
    }
}
