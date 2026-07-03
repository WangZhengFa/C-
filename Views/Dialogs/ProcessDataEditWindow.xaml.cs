using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 工序数据编辑窗口
    /// </summary>
    public partial class ProcessDataEditWindow : Window
    {
        public ProcessDataRecord Value { get; private set; }

        public ProcessDataEditWindow(ProcessDataRecord? source, IEnumerable<ProcessDataRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new ProcessDataRecord() : Clone(source);
            BindValue();
        }

        private void BindValue()
        {
            ProcessDataIdText.Text = Value.ProcessDataId;
            ProcessStepIdText.Text = Value.ProcessStepId;
            BatchNoText.Text = Value.BatchNo;
            ProductionDatePicker.SelectedDate = Value.ProductionDate;
            EndDatePicker.SelectedDate = Value.EndDate;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var processDataId = ProcessDataIdText.Text.Trim();
            var processStepId = ProcessStepIdText.Text.Trim();
            var batchNo = BatchNoText.Text.Trim();
            if (string.IsNullOrWhiteSpace(processDataId))
            {
                MessageBox.Show(this, "工序数据ID不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(processStepId))
            {
                MessageBox.Show(this, "工序步骤ID不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(batchNo))
            {
                MessageBox.Show(this, "批号不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.ProcessDataId = processDataId;
            Value.ProcessStepId = processStepId;
            Value.BatchNo = batchNo;
            Value.ProductionDate = ProductionDatePicker.SelectedDate;
            Value.EndDate = EndDatePicker.SelectedDate;
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static ProcessDataRecord Clone(ProcessDataRecord source)
        {
            return new ProcessDataRecord
            {
                Id = source.Id,
                ProcessDataId = source.ProcessDataId,
                ProcessStepId = source.ProcessStepId,
                BatchNo = source.BatchNo,
                ProductionDate = source.ProductionDate,
                EndDate = source.EndDate,
                Remark = source.Remark
            };
        }
    }
}
