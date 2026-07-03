using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 通用字典字段定义编辑窗口
    /// </summary>
    public partial class CommonDictFieldDefsEditWindow : Window
    {
        public CommonDictFieldDefsRecord Value { get; private set; }

        public CommonDictFieldDefsEditWindow(CommonDictFieldDefsRecord? source, IEnumerable<CommonDictFieldDefsRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new CommonDictFieldDefsRecord { IsEnabled = true, SortOrder = 0 } : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<CommonDictFieldDefsRecord> existing)
        {
            FieldTypeCombo.Items.Clear();
            FieldTypeCombo.Items.Add("text");
            FieldTypeCombo.Items.Add("number");
            FieldTypeCombo.Items.Add("date");
            FieldTypeCombo.Items.Add("bool");
            FieldTypeCombo.Items.Add("select");

            foreach (var value in existing.Select(x => x.FieldType).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!FieldTypeCombo.Items.Contains(value))
                {
                    FieldTypeCombo.Items.Add(value);
                }
            }
        }

        private void BindValue()
        {
            FieldKeyText.Text = Value.FieldKey;
            FieldLabelText.Text = Value.FieldLabel;
            FieldTypeCombo.Text = Value.FieldType;
            PlaceholderText.Text = Value.Placeholder;
            MinValueText.Text = Value.MinValue.ToString(CultureInfo.InvariantCulture);
            MaxValueText.Text = Value.MaxValue.ToString(CultureInfo.InvariantCulture);
            OptionsText.Text = Value.Options;
            DescriptionText.Text = Value.Description;
            SortOrderText.Text = Value.SortOrder.ToString(CultureInfo.InvariantCulture);
            IsEnabledCheck.IsChecked = Value.IsEnabled;
            NodeCodeText.Text = Value.NodeCode;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var fieldKey = FieldKeyText.Text.Trim();
            var fieldType = FieldTypeCombo.Text.Trim();
            if (string.IsNullOrWhiteSpace(fieldKey))
            {
                MessageBox.Show(this, "字段键不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(fieldType))
            {
                MessageBox.Show(this, "字段类型不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(MinValueText.Text) && !int.TryParse(MinValueText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var minValue))
            {
                MessageBox.Show(this, "最小值必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(MaxValueText.Text) && !int.TryParse(MaxValueText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxValue))
            {
                MessageBox.Show(this, "最大值必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(SortOrderText.Text) && !int.TryParse(SortOrderText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sortOrder))
            {
                MessageBox.Show(this, "排序必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.FieldKey = fieldKey;
            Value.FieldLabel = FieldLabelText.Text.Trim();
            Value.FieldType = fieldType;
            Value.Placeholder = PlaceholderText.Text.Trim();
            Value.MinValue = string.IsNullOrWhiteSpace(MinValueText.Text) ? -999999 : int.Parse(MinValueText.Text.Trim(), CultureInfo.InvariantCulture);
            Value.MaxValue = string.IsNullOrWhiteSpace(MaxValueText.Text) ? 999999 : int.Parse(MaxValueText.Text.Trim(), CultureInfo.InvariantCulture);
            Value.Options = OptionsText.Text.Trim();
            Value.Description = DescriptionText.Text.Trim();
            Value.SortOrder = string.IsNullOrWhiteSpace(SortOrderText.Text) ? 0 : int.Parse(SortOrderText.Text.Trim(), CultureInfo.InvariantCulture);
            Value.IsEnabled = IsEnabledCheck.IsChecked == true;
            Value.NodeCode = NodeCodeText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static CommonDictFieldDefsRecord Clone(CommonDictFieldDefsRecord source)
        {
            return new CommonDictFieldDefsRecord
            {
                Id = source.Id,
                FieldKey = source.FieldKey,
                FieldLabel = source.FieldLabel,
                FieldType = source.FieldType,
                Placeholder = source.Placeholder,
                MinValue = source.MinValue,
                MaxValue = source.MaxValue,
                Options = source.Options,
                Description = source.Description,
                SortOrder = source.SortOrder,
                IsEnabled = source.IsEnabled,
                NodeCode = source.NodeCode
            };
        }
    }
}
