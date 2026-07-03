using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 报告编号编辑窗口
    /// </summary>
    public partial class ReportNumberingEditWindow : Window
    {
        public ReportNumberingRecord Value { get; private set; }

        public ReportNumberingEditWindow(ReportNumberingRecord? source, IEnumerable<ReportNumberingRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new ReportNumberingRecord() : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<ReportNumberingRecord> existing)
        {
            SampleSourceCombo.Items.Clear();
            foreach (var source in existing.Select(x => x.SampleSource)
                                           .Where(x => !string.IsNullOrWhiteSpace(x))
                                           .Distinct()
                                           .OrderBy(x => x))
            {
                SampleSourceCombo.Items.Add(source);
            }

            ReportResultCombo.Items.Clear();
            ReportResultCombo.Items.Add("合格");
            ReportResultCombo.Items.Add("不合格");
            ReportResultCombo.Items.Add("待定");
        }

        private void BindValue()
        {
            ReportCodeText.Text = Value.ReportCode;
            ProductionDatePicker.SelectedDate = Value.ProductionDate;
            InspectionDatePicker.SelectedDate = Value.InspectionDate;
            ReportDatePicker.SelectedDate = Value.ReportDate;
            MaterialIdText.Text = Value.MaterialId;
            SampleBatchText.Text = Value.SampleBatch;
            SampleQuantityText.Text = Value.SampleQuantity;
            BatchQuantityText.Text = Value.BatchQuantity;
            SampleSourceCombo.Text = Value.SampleSource;
            ReportResultCombo.Text = Value.ReportResult;
            NodeCodeText.Text = Value.NodeCode;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var code = ReportCodeText.Text.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show(this, "报告编号不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.ReportCode = code;
            Value.ProductionDate = ProductionDatePicker.SelectedDate;
            Value.InspectionDate = InspectionDatePicker.SelectedDate;
            Value.ReportDate = ReportDatePicker.SelectedDate;
            Value.MaterialId = MaterialIdText.Text.Trim();
            Value.SampleBatch = SampleBatchText.Text.Trim();
            Value.SampleQuantity = SampleQuantityText.Text.Trim();
            Value.BatchQuantity = BatchQuantityText.Text.Trim();
            Value.SampleSource = SampleSourceCombo.Text.Trim();
            Value.ReportResult = ReportResultCombo.Text.Trim();
            Value.NodeCode = NodeCodeText.Text.Trim();
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static ReportNumberingRecord Clone(ReportNumberingRecord source)
        {
            return new ReportNumberingRecord
            {
                Id = source.Id,
                ReportCode = source.ReportCode,
                ProductionDate = source.ProductionDate,
                InspectionDate = source.InspectionDate,
                ReportDate = source.ReportDate,
                MaterialId = source.MaterialId,
                SampleBatch = source.SampleBatch,
                SampleQuantity = source.SampleQuantity,
                BatchQuantity = source.BatchQuantity,
                SampleSource = source.SampleSource,
                ReportResult = source.ReportResult,
                Remark = source.Remark,
                NodeCode = source.NodeCode
            };
        }
    }
}
