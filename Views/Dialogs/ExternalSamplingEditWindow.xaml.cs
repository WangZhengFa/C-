using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 外部抽检编辑窗口
    /// </summary>
    public partial class ExternalSamplingEditWindow : Window
    {
        public ExternalSamplingRecord Value { get; private set; }

        public ExternalSamplingEditWindow(ExternalSamplingRecord? source, IEnumerable<ExternalSamplingRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new ExternalSamplingRecord { SamplingDate = DateTime.Today } : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<ExternalSamplingRecord> existing)
        {
            MonitorTypeCombo.Items.Clear();
            MonitorTypeCombo.Items.Add("监督抽检");
            MonitorTypeCombo.Items.Add("风险监测");
            MonitorTypeCombo.Items.Add("委托抽检");

            SamplingOrgCombo.Items.Clear();
            foreach (var org in existing.Select(x => x.SamplingOrg)
                                        .Where(x => !string.IsNullOrWhiteSpace(x))
                                        .Distinct()
                                        .OrderBy(x => x))
            {
                SamplingOrgCombo.Items.Add(org);
            }
        }

        private void BindValue()
        {
            SamplingIdText.Text = Value.SamplingId;
            SamplingDatePicker.SelectedDate = Value.SamplingDate;
            ProductIdText.Text = Value.ProductId;
            BatchNoText.Text = Value.BatchNo;
            ProductQuantityText.Text = Value.ProductQuantity;
            SamplingQuantityText.Text = Value.SamplingQuantity;
            SamplingPriceText.Text = Value.SamplingPrice;
            MonitorTypeCombo.Text = Value.MonitorType;
            SamplingOrgCombo.Text = Value.SamplingOrg;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var samplingId = SamplingIdText.Text.Trim();
            var productId = ProductIdText.Text.Trim();
            if (string.IsNullOrWhiteSpace(samplingId))
            {
                MessageBox.Show(this, "抽检编号不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(productId))
            {
                MessageBox.Show(this, "产品编码不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.SamplingId = samplingId;
            Value.SamplingDate = SamplingDatePicker.SelectedDate;
            Value.ProductId = productId;
            Value.BatchNo = BatchNoText.Text.Trim();
            Value.ProductQuantity = ProductQuantityText.Text.Trim();
            Value.SamplingQuantity = SamplingQuantityText.Text.Trim();
            Value.SamplingPrice = SamplingPriceText.Text.Trim();
            Value.MonitorType = MonitorTypeCombo.Text.Trim();
            Value.SamplingOrg = SamplingOrgCombo.Text.Trim();
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static ExternalSamplingRecord Clone(ExternalSamplingRecord source)
        {
            return new ExternalSamplingRecord
            {
                Id = source.Id,
                SamplingId = source.SamplingId,
                SamplingDate = source.SamplingDate,
                ProductId = source.ProductId,
                BatchNo = source.BatchNo,
                ProductQuantity = source.ProductQuantity,
                SamplingQuantity = source.SamplingQuantity,
                SamplingPrice = source.SamplingPrice,
                MonitorType = source.MonitorType,
                SamplingOrg = source.SamplingOrg,
                Remark = source.Remark
            };
        }
    }
}
