using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 通用字典数据编辑窗口
    /// </summary>
    public partial class CommonDictDataEditWindow : Window
    {
        public CommonDictDataRecord Value { get; private set; }

        public CommonDictDataEditWindow(CommonDictDataRecord? source, IEnumerable<CommonDictDataRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new CommonDictDataRecord { IsEnabled = true, SortOrder = 1 } : Clone(source);
            BindValue();
        }

        private void BindValue()
        {
            NodeCodeText.Text = Value.NodeCode;
            CodeText.Text = Value.Code;
            NameText.Text = Value.Name;
            Field1Text.Text = Value.Field1;
            Field2Text.Text = Value.Field2;
            Field3Text.Text = Value.Field3;
            Field4Text.Text = Value.Field4;
            Field5Text.Text = Value.Field5;
            Number1Text.Text = Value.Number1?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            Number2Text.Text = Value.Number2?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            AmountText.Text = Value.Amount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            Date1Picker.SelectedDate = Value.Date1;
            Date2Picker.SelectedDate = Value.Date2;
            Flag1Check.IsChecked = Value.Flag1;
            Flag2Check.IsChecked = Value.Flag2;
            IsEnabledCheck.IsChecked = Value.IsEnabled;
            SortOrderText.Text = Value.SortOrder.ToString();
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var nodeCode = NodeCodeText.Text.Trim();
            var code = CodeText.Text.Trim();
            var name = NameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(nodeCode))
            {
                MessageBox.Show(this, "节点编码不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show(this, "编码不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, "名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(Number1Text.Text) && !decimal.TryParse(Number1Text.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var number1))
            {
                MessageBox.Show(this, "数值1格式不正确", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(Number2Text.Text) && !decimal.TryParse(Number2Text.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var number2))
            {
                MessageBox.Show(this, "数值2格式不正确", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(AmountText.Text) && !decimal.TryParse(AmountText.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            {
                MessageBox.Show(this, "金额格式不正确", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(SortOrderText.Text) && !int.TryParse(SortOrderText.Text.Trim(), out var sortOrder))
            {
                MessageBox.Show(this, "排序必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.NodeCode = nodeCode;
            Value.Code = code;
            Value.Name = name;
            Value.Field1 = Field1Text.Text.Trim();
            Value.Field2 = Field2Text.Text.Trim();
            Value.Field3 = Field3Text.Text.Trim();
            Value.Field4 = Field4Text.Text.Trim();
            Value.Field5 = Field5Text.Text.Trim();
            Value.Number1 = string.IsNullOrWhiteSpace(Number1Text.Text) ? null : decimal.Parse(Number1Text.Text.Trim(), CultureInfo.InvariantCulture);
            Value.Number2 = string.IsNullOrWhiteSpace(Number2Text.Text) ? null : decimal.Parse(Number2Text.Text.Trim(), CultureInfo.InvariantCulture);
            Value.Date1 = Date1Picker.SelectedDate;
            Value.Date2 = Date2Picker.SelectedDate;
            Value.Flag1 = Flag1Check.IsChecked == true;
            Value.Flag2 = Flag2Check.IsChecked == true;
            Value.Amount = string.IsNullOrWhiteSpace(AmountText.Text) ? null : decimal.Parse(AmountText.Text.Trim(), CultureInfo.InvariantCulture);
            Value.Remark = RemarkText.Text.Trim();
            Value.SortOrder = string.IsNullOrWhiteSpace(SortOrderText.Text) ? 1 : int.Parse(SortOrderText.Text.Trim());
            Value.IsEnabled = IsEnabledCheck.IsChecked == true;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static CommonDictDataRecord Clone(CommonDictDataRecord source)
        {
            return new CommonDictDataRecord
            {
                Id = source.Id,
                NodeCode = source.NodeCode,
                Code = source.Code,
                Name = source.Name,
                Field1 = source.Field1,
                Field2 = source.Field2,
                Field3 = source.Field3,
                Field4 = source.Field4,
                Field5 = source.Field5,
                Number1 = source.Number1,
                Number2 = source.Number2,
                Date1 = source.Date1,
                Date2 = source.Date2,
                Flag1 = source.Flag1,
                Flag2 = source.Flag2,
                Amount = source.Amount,
                Remark = source.Remark,
                SortOrder = source.SortOrder,
                IsEnabled = source.IsEnabled
            };
        }
    }
}
