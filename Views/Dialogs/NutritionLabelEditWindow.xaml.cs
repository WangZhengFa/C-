using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 营养标签编辑窗口
    /// </summary>
    public partial class NutritionLabelEditWindow : Window
    {
        public NutritionLabelRecord Value { get; private set; }
        private readonly bool _isEdit;

        public NutritionLabelEditWindow(NutritionLabelRecord? source, IEnumerable<NutritionLabelRecord> existing)
        {
            InitializeComponent();
            _isEdit = source != null;
            Value = source == null ? new NutritionLabelRecord() : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<NutritionLabelRecord> existing)
        {
            DetectionModeCombo.Items.Clear();
            DetectionModeCombo.Items.Add("core");
            DetectionModeCombo.Items.Add("extended");

            foreach (var mode in existing.Select(x => x.DetectionMode)
                                         .Where(x => !string.IsNullOrWhiteSpace(x))
                                         .Distinct()
                                         .OrderBy(x => x))
            {
                if (!DetectionModeCombo.Items.Contains(mode))
                {
                    DetectionModeCombo.Items.Add(mode);
                }
            }
        }

        private void BindValue()
        {
            LabelIdText.Text = Value.LabelId;
            LabelIdText.IsEnabled = !_isEdit;
            NameText.Text = Value.Name;
            DetectionModeCombo.Text = string.IsNullOrWhiteSpace(Value.DetectionMode) ? "core" : Value.DetectionMode;
            HeavyMetalCheck.IsChecked = Value.HeavyMetal;
            ClaimTypesText.Text = Value.ClaimTypes;
            NutrientDataText.Text = Value.NutrientData;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var labelId = LabelIdText.Text.Trim();
            var name = NameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(labelId))
            {
                MessageBox.Show(this, "标签ID不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, "标签名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.LabelId = labelId;
            Value.Name = name;
            Value.DetectionMode = DetectionModeCombo.Text.Trim();
            Value.HeavyMetal = HeavyMetalCheck.IsChecked == true;
            Value.ClaimTypes = ClaimTypesText.Text.Trim();
            Value.NutrientData = NutrientDataText.Text.Trim();
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static NutritionLabelRecord Clone(NutritionLabelRecord source)
        {
            return new NutritionLabelRecord
            {
                LabelId = source.LabelId,
                Name = source.Name,
                DetectionMode = source.DetectionMode,
                HeavyMetal = source.HeavyMetal,
                ClaimTypes = source.ClaimTypes,
                NutrientData = source.NutrientData,
                Remark = source.Remark
            };
        }
    }
}
