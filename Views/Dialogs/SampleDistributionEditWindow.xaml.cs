using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 样品分发编辑窗口
    /// </summary>
    public partial class SampleDistributionEditWindow : Window
    {
        public SampleDistributionRecord Value { get; private set; }
        public bool IsNew { get; private set; }

        private SampleDistributionRecord? _originalSource;
        private readonly IEnumerable<SampleDistributionRecord> _existing;
        private readonly SampleDistributionService _service = new();

        public SampleDistributionEditWindow(SampleDistributionRecord? source, IEnumerable<SampleDistributionRecord> existing)
        {
            InitializeComponent();
            _originalSource = source;
            _existing = existing;
            IsNew = source == null;
            Value = source == null ? CreateNewRecord() : Clone(source);
            InitCombos();
            BindValue();
        }

        private static SampleDistributionRecord CreateNewRecord()
        {
            return new SampleDistributionRecord
            {
                ReceiveSendDate = DateTime.Today
            };
        }

        private void InitCombos()
        {
            SampleSourceCombo.Items.Clear();
            foreach (var source in _existing.Select(x => x.SampleSource)
                                           .Where(x => !string.IsNullOrWhiteSpace(x))
                                           .Distinct()
                                           .OrderBy(x => x))
            {
                SampleSourceCombo.Items.Add(source);
            }

            var defaults = new[] { "生产企业", "流通环节", "餐饮环节", "网络抽样", "其他" };
            foreach (var d in defaults)
            {
                if (!SampleSourceCombo.Items.Contains(d))
                {
                    SampleSourceCombo.Items.Add(d);
                }
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

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            IsNew = true;
            _originalSource = null;
            Value = CreateNewRecord();
            Value.ReceiveSendId = GenerateReceiveSendId();
            BindValue();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_originalSource != null)
            {
                Value = Clone(_originalSource);
                IsNew = false;
            }
            else
            {
                Value = CreateNewRecord();
                Value.ReceiveSendId = GenerateReceiveSendId();
                IsNew = true;
            }
            BindValue();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var id = ReceiveSendIdText.Text.Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                MessageBox.Show(this, "分发ID不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sampleName = SampleNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(sampleName))
            {
                MessageBox.Show(this, "检品名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private string GenerateReceiveSendId()
        {
            var prefix = $"FY-{DateTime.Today:yyyyMMdd}-";
            var maxSeq = _existing
                .Where(x => !string.IsNullOrWhiteSpace(x.ReceiveSendId) && x.ReceiveSendId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(x =>
                {
                    var suffix = x.ReceiveSendId.Substring(prefix.Length);
                    return int.TryParse(suffix, out var n) ? n : 0;
                })
                .DefaultIfEmpty(0)
                .Max();
            return $"{prefix}{(maxSeq + 1):D3}";
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
