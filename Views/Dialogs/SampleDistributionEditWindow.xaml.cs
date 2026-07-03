using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 样品分发编辑窗口
    /// </summary>
    public partial class SampleDistributionEditWindow : Window
    {
        public SampleDistributionRecord Value { get; private set; }

        public SampleDistributionEditWindow(SampleDistributionRecord? source, IEnumerable<SampleDistributionRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new SampleDistributionRecord { ReceiveSendDate = DateTime.Today } : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<SampleDistributionRecord> existing)
        {
            SampleSourceCombo.Items.Clear();
            foreach (var source in existing.Select(x => x.SampleSource)
                                           .Where(x => !string.IsNullOrWhiteSpace(x))
                                           .Distinct()
                                           .OrderBy(x => x))
            {
                SampleSourceCombo.Items.Add(source);
            }
        }

        private void BindValue()
        {
            ReceiveSendIdText.Text = Value.ReceiveSendId;
            ReceiveSendDatePicker.SelectedDate = Value.ReceiveSendDate;
            InspectionDatePicker.SelectedDate = Value.InspectionDate;
            ReportDatePicker.SelectedDate = Value.ReportDate;
            SampleNameText.Text = Value.SampleName;
            SampleBatchText.Text = Value.SampleBatch;
            SampleQuantityText.Text = Value.SampleQuantity;
            RetentionQuantityText.Text = Value.RetentionQuantity;
            RepresentativeQuantityText.Text = Value.RepresentativeQuantity;
            SampleSourceCombo.Text = Value.SampleSource;
            IsReinspectionCheck.IsChecked = Value.IsReinspection;
            NodeCodeText.Text = Value.NodeCode;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var id = ReceiveSendIdText.Text.Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                MessageBox.Show(this, "收发编号不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sampleName = SampleNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(sampleName))
            {
                MessageBox.Show(this, "样品名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.ReceiveSendId = id;
            Value.ReceiveSendDate = ReceiveSendDatePicker.SelectedDate;
            Value.InspectionDate = InspectionDatePicker.SelectedDate;
            Value.ReportDate = ReportDatePicker.SelectedDate;
            Value.SampleName = sampleName;
            Value.SampleBatch = SampleBatchText.Text.Trim();
            Value.SampleQuantity = SampleQuantityText.Text.Trim();
            Value.RetentionQuantity = RetentionQuantityText.Text.Trim();
            Value.RepresentativeQuantity = RepresentativeQuantityText.Text.Trim();
            Value.SampleSource = SampleSourceCombo.Text.Trim();
            Value.IsReinspection = IsReinspectionCheck.IsChecked == true;
            Value.NodeCode = NodeCodeText.Text.Trim();
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static SampleDistributionRecord Clone(SampleDistributionRecord source)
        {
            return new SampleDistributionRecord
            {
                Id = source.Id,
                ReceiveSendId = source.ReceiveSendId,
                ReceiveSendDate = source.ReceiveSendDate,
                InspectionDate = source.InspectionDate,
                ReportDate = source.ReportDate,
                SampleName = source.SampleName,
                SampleBatch = source.SampleBatch,
                SampleQuantity = source.SampleQuantity,
                RetentionQuantity = source.RetentionQuantity,
                RepresentativeQuantity = source.RepresentativeQuantity,
                SampleSource = source.SampleSource,
                IsReinspection = source.IsReinspection,
                Remark = source.Remark,
                NodeCode = source.NodeCode
            };
        }
    }
}
