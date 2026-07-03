using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 报告数据编辑窗口
    /// </summary>
    public partial class ReportDataEditWindow : Window
    {
        public ReportDataRecord Value { get; private set; }

        public ReportDataEditWindow(ReportDataRecord? source, IEnumerable<ReportDataRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new ReportDataRecord() : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<ReportDataRecord> existing)
        {
            TypeCombo.Items.Clear();
            TypeCombo.Items.Add("例行检测");
            TypeCombo.Items.Add("委托检测");
            TypeCombo.Items.Add("监督检测");

            FrequencyCombo.Items.Clear();
            FrequencyCombo.Items.Add("月度");
            FrequencyCombo.Items.Add("季度");
            FrequencyCombo.Items.Add("年度");
            FrequencyCombo.Items.Add("临时");

            ConclusionCombo.Items.Clear();
            ConclusionCombo.Items.Add("合格");
            ConclusionCombo.Items.Add("不合格");
            ConclusionCombo.Items.Add("待定");

            foreach (var value in existing.Select(x => x.Type).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!TypeCombo.Items.Contains(value))
                {
                    TypeCombo.Items.Add(value);
                }
            }

            foreach (var value in existing.Select(x => x.Frequency).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!FrequencyCombo.Items.Contains(value))
                {
                    FrequencyCombo.Items.Add(value);
                }
            }

            foreach (var value in existing.Select(x => x.Conclusion).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!ConclusionCombo.Items.Contains(value))
                {
                    ConclusionCombo.Items.Add(value);
                }
            }
        }

        private void BindValue()
        {
            NodeCodeText.Text = Value.NodeCode;
            ReportNumberText.Text = Value.ReportNumber;
            SampleNameText.Text = Value.SampleName;
            SampleBatchText.Text = Value.SampleBatch;
            TypeCombo.Text = Value.Type;
            FrequencyCombo.Text = Value.Frequency;
            TestingInstitutionText.Text = Value.TestingInstitution;
            TestingDatePicker.SelectedDate = Value.TestingDate;
            ReportDatePicker.SelectedDate = Value.ReportDate;
            ConclusionCombo.Text = Value.Conclusion;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var reportNumber = ReportNumberText.Text.Trim();
            var sampleName = SampleNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(reportNumber))
            {
                MessageBox.Show(this, "报告编号不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(sampleName))
            {
                MessageBox.Show(this, "样品名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.NodeCode = NodeCodeText.Text.Trim();
            Value.ReportNumber = reportNumber;
            Value.SampleName = sampleName;
            Value.SampleBatch = SampleBatchText.Text.Trim();
            Value.Type = TypeCombo.Text.Trim();
            Value.Frequency = FrequencyCombo.Text.Trim();
            Value.TestingInstitution = TestingInstitutionText.Text.Trim();
            Value.TestingDate = TestingDatePicker.SelectedDate;
            Value.ReportDate = ReportDatePicker.SelectedDate;
            Value.Conclusion = ConclusionCombo.Text.Trim();
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static ReportDataRecord Clone(ReportDataRecord source)
        {
            return new ReportDataRecord
            {
                Id = source.Id,
                NodeCode = source.NodeCode,
                ReportNumber = source.ReportNumber,
                SampleName = source.SampleName,
                SampleBatch = source.SampleBatch,
                Type = source.Type,
                Frequency = source.Frequency,
                TestingInstitution = source.TestingInstitution,
                TestingDate = source.TestingDate,
                ReportDate = source.ReportDate,
                Conclusion = source.Conclusion,
                Remark = source.Remark
            };
        }
    }
}
