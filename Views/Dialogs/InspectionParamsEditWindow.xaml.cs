using System.Collections.Generic;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 检品参数编辑窗口
    /// </summary>
    public partial class InspectionParamsEditWindow : Window
    {
        public InspectionParamsRecord Value { get; private set; }

        public InspectionParamsEditWindow(InspectionParamsRecord? source, IEnumerable<InspectionParamsRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new InspectionParamsRecord() : Clone(source);
            BindValue();
        }

        private void BindValue()
        {
            InspectionIdText.Text = Value.InspectionId;
            NodeCodeText.Text = Value.NodeCode;
            InspectionNameText.Text = Value.InspectionName;
            MaterialCodeText.Text = Value.MaterialCode;
            StandardText.Text = Value.Standard;
            SpecificationText.Text = Value.Specification;
            RemarkText.Text = Value.Remark;
            IsDisabledCheck.IsChecked = Value.IsDisabled;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var inspectionId = InspectionIdText.Text.Trim();
            var nodeCode = NodeCodeText.Text.Trim();
            var inspectionName = InspectionNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(inspectionId))
            {
                MessageBox.Show(this, "检品编号不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(nodeCode))
            {
                MessageBox.Show(this, "节点编码不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(inspectionName))
            {
                MessageBox.Show(this, "检品名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.InspectionId = inspectionId;
            Value.NodeCode = nodeCode;
            Value.InspectionName = inspectionName;
            Value.MaterialCode = MaterialCodeText.Text.Trim();
            Value.Standard = StandardText.Text.Trim();
            Value.Specification = SpecificationText.Text.Trim();
            Value.Remark = RemarkText.Text.Trim();
            Value.IsDisabled = IsDisabledCheck.IsChecked == true;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static InspectionParamsRecord Clone(InspectionParamsRecord source)
        {
            return new InspectionParamsRecord
            {
                Id = source.Id,
                InspectionId = source.InspectionId,
                NodeCode = source.NodeCode,
                InspectionName = source.InspectionName,
                MaterialCode = source.MaterialCode,
                Standard = source.Standard,
                Specification = source.Specification,
                IsDisabled = source.IsDisabled,
                Remark = source.Remark
            };
        }
    }
}
