using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 物料信息编辑窗口
    /// </summary>
    public partial class MaterialInfoEditWindow : Window
    {
        public MaterialInfoRecord Value { get; private set; }

        public MaterialInfoEditWindow(MaterialInfoRecord? source, IEnumerable<MaterialInfoRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new MaterialInfoRecord { IsDisabled = false } : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<MaterialInfoRecord> existing)
        {
            BrandSeriesCombo.Items.Clear();
            UnitCombo.Items.Clear();
            BrandSeriesCombo.Items.Add("标准版");
            BrandSeriesCombo.Items.Add("定制版");
            UnitCombo.Items.Add("kg");
            UnitCombo.Items.Add("g");
            UnitCombo.Items.Add("L");
            UnitCombo.Items.Add("mL");
            UnitCombo.Items.Add("个");

            foreach (var value in existing.Select(x => x.BrandSeries).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!BrandSeriesCombo.Items.Contains(value))
                {
                    BrandSeriesCombo.Items.Add(value);
                }
            }

            foreach (var value in existing.Select(x => x.Unit).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!UnitCombo.Items.Contains(value))
                {
                    UnitCombo.Items.Add(value);
                }
            }
        }

        private void BindValue()
        {
            MaterialIdText.Text = Value.MaterialId;
            NodeCodeText.Text = Value.NodeCode;
            FirstLevelCodeText.Text = Value.FirstLevelCode;
            MaterialCodeText.Text = Value.MaterialCode;
            MaterialNameText.Text = Value.MaterialName;
            SpecificationText.Text = Value.Specification;
            PackagingSpecText.Text = Value.PackagingSpec;
            BrandSeriesCombo.Text = Value.BrandSeries;
            ExpiryDateText.Text = Value.ExpiryDate <= 0 ? string.Empty : Value.ExpiryDate.ToString();
            UnitCombo.Text = Value.Unit;
            StandardText.Text = Value.Standard;
            IsDisabledCheck.IsChecked = Value.IsDisabled;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var materialName = MaterialNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(materialName))
            {
                MessageBox.Show(this, "物料名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(ExpiryDateText.Text) && !int.TryParse(ExpiryDateText.Text.Trim(), out var expiryDate))
            {
                MessageBox.Show(this, "有效期必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.MaterialId = MaterialIdText.Text.Trim();
            Value.NodeCode = NodeCodeText.Text.Trim();
            Value.FirstLevelCode = FirstLevelCodeText.Text.Trim();
            Value.MaterialCode = MaterialCodeText.Text.Trim();
            Value.MaterialName = materialName;
            Value.Specification = SpecificationText.Text.Trim();
            Value.PackagingSpec = PackagingSpecText.Text.Trim();
            Value.BrandSeries = BrandSeriesCombo.Text.Trim();
            Value.ExpiryDate = string.IsNullOrWhiteSpace(ExpiryDateText.Text) ? 0 : int.Parse(ExpiryDateText.Text.Trim());
            Value.Unit = UnitCombo.Text.Trim();
            Value.Standard = StandardText.Text.Trim();
            Value.IsDisabled = IsDisabledCheck.IsChecked == true;
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static MaterialInfoRecord Clone(MaterialInfoRecord source)
        {
            return new MaterialInfoRecord
            {
                Id = source.Id,
                MaterialId = source.MaterialId,
                NodeCode = source.NodeCode,
                FirstLevelCode = source.FirstLevelCode,
                MaterialCode = source.MaterialCode,
                MaterialName = source.MaterialName,
                Specification = source.Specification,
                PackagingSpec = source.PackagingSpec,
                BrandSeries = source.BrandSeries,
                ExpiryDate = source.ExpiryDate,
                Unit = source.Unit,
                Standard = source.Standard,
                IsDisabled = source.IsDisabled,
                Remark = source.Remark
            };
        }
    }
}
