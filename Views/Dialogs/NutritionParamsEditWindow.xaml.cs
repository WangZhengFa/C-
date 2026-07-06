using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 营养参数编辑窗口
    /// </summary>
    public partial class NutritionParamsEditWindow : Window
    {
        public NutritionParamsRecord Value { get; private set; }

        public NutritionParamsEditWindow(NutritionParamsRecord? source, IEnumerable<NutritionParamsRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new NutritionParamsRecord { SortOrder = 0, Disabled = false } : Clone(source);
            BindValue();
        }

        private void BindValue()
        {
            SortOrderText.Text = Value.SortOrder.ToString(CultureInfo.InvariantCulture);
            NutrientText.Text = Value.Nutrient;
            UnitText.Text = Value.Unit;
            RoundingIntervalText.Text = Value.RoundingInterval?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ZeroThresholdText.Text = Value.ZeroThreshold?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            EnergyValueText.Text = Value.EnergyValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            LowerErrorText.Text = Value.LowerError?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            UpperErrorText.Text = Value.UpperError?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            NrvText.Text = Value.Nrv?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            CoreCheck.IsChecked = Value.Core;
            HeavyMetalCheck.IsChecked = Value.HeavyMetal;
            IsSportsNutritionCheck.IsChecked = Value.IsSportsNutrition;
            DailyUsageRangeText.Text = Value.DailyUsageRange;
            DisabledCheck.IsChecked = Value.Disabled;
            DescriptionText.Text = Value.Description;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var nutrient = NutrientText.Text.Trim();
            if (string.IsNullOrWhiteSpace(nutrient))
            {
                MessageBox.Show(this, "营养素不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(SortOrderText.Text) && !int.TryParse(SortOrderText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sortOrder))
            {
                MessageBox.Show(this, "排序必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(RoundingIntervalText.Text) && !decimal.TryParse(RoundingIntervalText.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var roundingInterval))
            {
                MessageBox.Show(this, "四舍五入间隔格式不正确", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(ZeroThresholdText.Text) && !decimal.TryParse(ZeroThresholdText.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var zeroThreshold))
            {
                MessageBox.Show(this, "零值阈值格式不正确", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(EnergyValueText.Text) && !decimal.TryParse(EnergyValueText.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var energyValue))
            {
                MessageBox.Show(this, "能量值格式不正确", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(LowerErrorText.Text) && !decimal.TryParse(LowerErrorText.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var lowerError))
            {
                MessageBox.Show(this, "下限误差格式不正确", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(UpperErrorText.Text) && !decimal.TryParse(UpperErrorText.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var upperError))
            {
                MessageBox.Show(this, "上限误差格式不正确", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(NrvText.Text) && !decimal.TryParse(NrvText.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var nrv))
            {
                MessageBox.Show(this, "NRV格式不正确", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.SortOrder = string.IsNullOrWhiteSpace(SortOrderText.Text) ? 0 : int.Parse(SortOrderText.Text.Trim(), CultureInfo.InvariantCulture);
            Value.Nutrient = nutrient;
            Value.Unit = UnitText.Text.Trim();
            Value.RoundingInterval = string.IsNullOrWhiteSpace(RoundingIntervalText.Text) ? null : decimal.Parse(RoundingIntervalText.Text.Trim(), CultureInfo.InvariantCulture);
            Value.ZeroThreshold = string.IsNullOrWhiteSpace(ZeroThresholdText.Text) ? null : decimal.Parse(ZeroThresholdText.Text.Trim(), CultureInfo.InvariantCulture);
            Value.EnergyValue = string.IsNullOrWhiteSpace(EnergyValueText.Text) ? null : decimal.Parse(EnergyValueText.Text.Trim(), CultureInfo.InvariantCulture);
            Value.LowerError = string.IsNullOrWhiteSpace(LowerErrorText.Text) ? null : decimal.Parse(LowerErrorText.Text.Trim(), CultureInfo.InvariantCulture);
            Value.UpperError = string.IsNullOrWhiteSpace(UpperErrorText.Text) ? null : decimal.Parse(UpperErrorText.Text.Trim(), CultureInfo.InvariantCulture);
            Value.Nrv = string.IsNullOrWhiteSpace(NrvText.Text) ? null : decimal.Parse(NrvText.Text.Trim(), CultureInfo.InvariantCulture);
            Value.Core = CoreCheck.IsChecked == true;
            Value.HeavyMetal = HeavyMetalCheck.IsChecked == true;
            Value.IsSportsNutrition = IsSportsNutritionCheck.IsChecked == true;
            Value.DailyUsageRange = DailyUsageRangeText.Text.Trim();
            Value.Disabled = DisabledCheck.IsChecked == true;
            Value.Description = DescriptionText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static NutritionParamsRecord Clone(NutritionParamsRecord source)
        {
            return new NutritionParamsRecord
            {
                Id = source.Id,
                SortOrder = source.SortOrder,
                Nutrient = source.Nutrient,
                Unit = source.Unit,
                RoundingInterval = source.RoundingInterval,
                ZeroThreshold = source.ZeroThreshold,
                EnergyValue = source.EnergyValue,
                LowerError = source.LowerError,
                UpperError = source.UpperError,
                Nrv = source.Nrv,
                Core = source.Core,
                HeavyMetal = source.HeavyMetal,
                IsSportsNutrition = source.IsSportsNutrition,
                DailyUsageRange = source.DailyUsageRange,
                Disabled = source.Disabled,
                Description = source.Description,
                UpdatedAt = source.UpdatedAt
            };
        }
    }
}
