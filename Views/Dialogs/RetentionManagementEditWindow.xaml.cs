using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 留样管理编辑窗口
    /// </summary>
    public partial class RetentionManagementEditWindow : Window
    {
        public RetentionManagementRecord Value { get; private set; }

        public RetentionManagementEditWindow(RetentionManagementRecord? source, IEnumerable<RetentionManagementRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new RetentionManagementRecord() : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<RetentionManagementRecord> existing)
        {
            SampleStatusCombo.Items.Clear();
            SampleStatusCombo.Items.Add("在库");
            SampleStatusCombo.Items.Add("已处置");
            SampleStatusCombo.Items.Add("待处置");

            foreach (var status in existing.Select(x => x.SampleStatus)
                                           .Where(x => !string.IsNullOrWhiteSpace(x))
                                           .Distinct()
                                           .OrderBy(x => x))
            {
                if (!SampleStatusCombo.Items.Contains(status))
                {
                    SampleStatusCombo.Items.Add(status);
                }
            }
        }

        private void BindValue()
        {
            RetentionCodeText.Text = Value.RetentionCode;
            ReportCodeText.Text = Value.ReportCode;
            MaterialIdText.Text = Value.MaterialId;
            BatchNumberText.Text = Value.BatchNumber;
            RetentionDatePicker.SelectedDate = Value.RetentionDate;
            RetentionPersonText.Text = Value.RetentionPerson;
            RetentionDeadlinePicker.SelectedDate = Value.RetentionDeadline;
            RetentionLocationText.Text = Value.RetentionLocation;
            RetentionQuantityText.Text = Value.RetentionQuantity <= 0 ? string.Empty : Value.RetentionQuantity.ToString();
            StorageConditionText.Text = Value.StorageCondition;
            SampleStatusCombo.Text = string.IsNullOrWhiteSpace(Value.SampleStatus) ? "在库" : Value.SampleStatus;
            DisposeDatePicker.SelectedDate = Value.DisposeDate;
            DisposePersonText.Text = Value.DisposePerson;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var code = RetentionCodeText.Text.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show(this, "留样编号不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var quantityText = RetentionQuantityText.Text.Trim();
            var quantity = 0;
            if (!string.IsNullOrWhiteSpace(quantityText) && !int.TryParse(quantityText, out quantity))
            {
                MessageBox.Show(this, "留样数量必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.RetentionCode = code;
            Value.ReportCode = ReportCodeText.Text.Trim();
            Value.MaterialId = MaterialIdText.Text.Trim();
            Value.BatchNumber = BatchNumberText.Text.Trim();
            Value.RetentionDate = RetentionDatePicker.SelectedDate;
            Value.RetentionPerson = RetentionPersonText.Text.Trim();
            Value.RetentionDeadline = RetentionDeadlinePicker.SelectedDate;
            Value.RetentionLocation = RetentionLocationText.Text.Trim();
            Value.RetentionQuantity = quantity;
            Value.StorageCondition = StorageConditionText.Text.Trim();
            Value.SampleStatus = SampleStatusCombo.Text.Trim();
            Value.DisposeDate = DisposeDatePicker.SelectedDate;
            Value.DisposePerson = DisposePersonText.Text.Trim();
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static RetentionManagementRecord Clone(RetentionManagementRecord source)
        {
            return new RetentionManagementRecord
            {
                Id = source.Id,
                RetentionCode = source.RetentionCode,
                ReportCode = source.ReportCode,
                MaterialId = source.MaterialId,
                BatchNumber = source.BatchNumber,
                RetentionDate = source.RetentionDate,
                RetentionPerson = source.RetentionPerson,
                RetentionDeadline = source.RetentionDeadline,
                RetentionLocation = source.RetentionLocation,
                RetentionQuantity = source.RetentionQuantity,
                StorageCondition = source.StorageCondition,
                SampleStatus = source.SampleStatus,
                DisposeDate = source.DisposeDate,
                DisposePerson = source.DisposePerson,
                Remark = source.Remark
            };
        }
    }
}
