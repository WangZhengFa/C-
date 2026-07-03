using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 型式检验编辑窗口
    /// </summary>
    public partial class TypeInspectionEditWindow : Window
    {
        public TypeInspectionRecord Value { get; private set; }

        public TypeInspectionEditWindow(TypeInspectionRecord? source, IEnumerable<TypeInspectionRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new TypeInspectionRecord { SendDate = DateTime.Today, Conclusion = "合格" } : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<TypeInspectionRecord> existing)
        {
            ConclusionCombo.Items.Clear();
            ConclusionCombo.Items.Add("合格");
            ConclusionCombo.Items.Add("不合格");
            ConclusionCombo.Items.Add("待定");

            TestingOrgCombo.Items.Clear();
            foreach (var org in existing.Select(x => x.TestingOrg)
                                       .Where(x => !string.IsNullOrWhiteSpace(x))
                                       .Distinct()
                                       .OrderBy(x => x))
            {
                TestingOrgCombo.Items.Add(org);
            }
        }

        private void BindValue()
        {
            InspectionIdText.Text = Value.InspectionId;
            ProductIdText.Text = Value.ProductId;
            BatchNoText.Text = Value.BatchNo;
            SendDatePicker.SelectedDate = Value.SendDate;
            ReportDatePicker.SelectedDate = Value.ReportDate;
            ConclusionCombo.Text = string.IsNullOrWhiteSpace(Value.Conclusion) ? "合格" : Value.Conclusion;
            TestingOrgCombo.Text = Value.TestingOrg;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var inspectionId = InspectionIdText.Text.Trim();
            var productId = ProductIdText.Text.Trim();
            if (string.IsNullOrWhiteSpace(inspectionId))
            {
                MessageBox.Show(this, "检验编号不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(productId))
            {
                MessageBox.Show(this, "产品编码不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.InspectionId = inspectionId;
            Value.ProductId = productId;
            Value.BatchNo = BatchNoText.Text.Trim();
            Value.SendDate = SendDatePicker.SelectedDate;
            Value.ReportDate = ReportDatePicker.SelectedDate;
            Value.Conclusion = string.IsNullOrWhiteSpace(ConclusionCombo.Text) ? "合格" : ConclusionCombo.Text.Trim();
            Value.TestingOrg = TestingOrgCombo.Text.Trim();
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static TypeInspectionRecord Clone(TypeInspectionRecord source)
        {
            return new TypeInspectionRecord
            {
                Id = source.Id,
                InspectionId = source.InspectionId,
                ProductId = source.ProductId,
                BatchNo = source.BatchNo,
                SendDate = source.SendDate,
                ReportDate = source.ReportDate,
                Conclusion = source.Conclusion,
                TestingOrg = source.TestingOrg,
                Remark = source.Remark
            };
        }
    }
}
