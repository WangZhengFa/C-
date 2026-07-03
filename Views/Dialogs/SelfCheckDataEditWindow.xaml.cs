using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 自检数据编辑窗口
    /// </summary>
    public partial class SelfCheckDataEditWindow : Window
    {
        public SelfCheckDataRecord Value { get; private set; }

        public SelfCheckDataEditWindow(SelfCheckDataRecord? source, IEnumerable<SelfCheckDataRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new SelfCheckDataRecord { SamplingDate = DateTime.Today } : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<SelfCheckDataRecord> existing)
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
            SelfCheckIdText.Text = Value.SelfCheckId;
            SamplingDatePicker.SelectedDate = Value.SamplingDate;
            ReportDatePicker.SelectedDate = Value.ReportDate;
            InspectionIdText.Text = Value.InspectionId;
            SampleBatchText.Text = Value.SampleBatch;
            BrandSeriesText.Text = Value.BrandSeries;
            SampleQuantityText.Text = Value.SampleQuantity;
            RepresentativeQuantityText.Text = Value.RepresentativeQuantity;
            SampleSourceCombo.Text = Value.SampleSource;
            NodeCodeText.Text = Value.NodeCode;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var id = SelfCheckIdText.Text.Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                MessageBox.Show(this, "自检编号不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.SelfCheckId = id;
            Value.SamplingDate = SamplingDatePicker.SelectedDate;
            Value.ReportDate = ReportDatePicker.SelectedDate;
            Value.InspectionId = InspectionIdText.Text.Trim();
            Value.SampleBatch = SampleBatchText.Text.Trim();
            Value.BrandSeries = BrandSeriesText.Text.Trim();
            Value.SampleQuantity = SampleQuantityText.Text.Trim();
            Value.RepresentativeQuantity = RepresentativeQuantityText.Text.Trim();
            Value.SampleSource = SampleSourceCombo.Text.Trim();
            Value.NodeCode = NodeCodeText.Text.Trim();
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static SelfCheckDataRecord Clone(SelfCheckDataRecord source)
        {
            return new SelfCheckDataRecord
            {
                Id = source.Id,
                SelfCheckId = source.SelfCheckId,
                SamplingDate = source.SamplingDate,
                ReportDate = source.ReportDate,
                InspectionId = source.InspectionId,
                SampleBatch = source.SampleBatch,
                BrandSeries = source.BrandSeries,
                SampleQuantity = source.SampleQuantity,
                RepresentativeQuantity = source.RepresentativeQuantity,
                SampleSource = source.SampleSource,
                Remark = source.Remark,
                NodeCode = source.NodeCode
            };
        }
    }
}
