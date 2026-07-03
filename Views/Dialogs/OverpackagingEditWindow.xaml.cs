using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 过度包装编辑窗口
    /// </summary>
    public partial class OverpackagingEditWindow : Window
    {
        public OverpackagingRecord Value { get; private set; }

        public OverpackagingEditWindow(OverpackagingRecord? source, IEnumerable<OverpackagingRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new OverpackagingRecord() : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<OverpackagingRecord> existing)
        {
            ConclusionCombo.Items.Clear();
            ConclusionCombo.Items.Add("合格");
            ConclusionCombo.Items.Add("不合格");
            ConclusionCombo.Items.Add("待定");

            foreach (var value in existing.Select(x => x.Conclusion)
                                          .Where(x => !string.IsNullOrWhiteSpace(x))
                                          .Distinct()
                                          .OrderBy(x => x))
            {
                if (!ConclusionCombo.Items.Contains(value))
                {
                    ConclusionCombo.Items.Add(value);
                }
            }
        }

        private void BindValue()
        {
            TestIdText.Text = Value.TestId;
            TestDatePicker.SelectedDate = Value.TestDate;
            ProductNameText.Text = Value.ProductName;
            BrandSeriesText.Text = Value.BrandSeries;
            ShapeTypeText.Text = Value.ShapeType;
            DimensionsText.Text = Value.Dimensions;
            PackageLayersText.Text = Value.PackageLayers <= 0 ? string.Empty : Value.PackageLayers.ToString();
            PackageWeightText.Text = Value.PackageWeight <= 0m ? string.Empty : Value.PackageWeight.ToString();
            PackageCostText.Text = Value.PackageCost <= 0m ? string.Empty : Value.PackageCost.ToString();
            SalesPriceText.Text = Value.SalesPrice <= 0m ? string.Empty : Value.SalesPrice.ToString();
            MaterialText.Text = Value.Material;
            IsMixedCheck.IsChecked = Value.IsMixed;
            IsFreezeDriedCheck.IsChecked = Value.IsFreezeDried;
            ProcessTypeText.Text = Value.ProcessType;
            ConclusionCombo.Text = Value.Conclusion;
            InnerItemsJsonText.Text = Value.InnerItemsJson;
            RemarksText.Text = Value.Remarks;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var testId = TestIdText.Text.Trim();
            if (string.IsNullOrWhiteSpace(testId))
            {
                MessageBox.Show(this, "检测编号不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse((PackageLayersText.Text ?? string.Empty).Trim(), out var layers))
            {
                MessageBox.Show(this, "包装层数必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!decimal.TryParse((PackageWeightText.Text ?? string.Empty).Trim(), out var weight))
            {
                MessageBox.Show(this, "包装重量必须是数字", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!decimal.TryParse((PackageCostText.Text ?? string.Empty).Trim(), out var cost))
            {
                MessageBox.Show(this, "包装成本必须是数字", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!decimal.TryParse((SalesPriceText.Text ?? string.Empty).Trim(), out var price))
            {
                MessageBox.Show(this, "销售价格必须是数字", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.TestId = testId;
            Value.TestDate = TestDatePicker.SelectedDate;
            Value.ProductName = ProductNameText.Text.Trim();
            Value.BrandSeries = BrandSeriesText.Text.Trim();
            Value.ShapeType = ShapeTypeText.Text.Trim();
            Value.Dimensions = DimensionsText.Text.Trim();
            Value.PackageLayers = layers;
            Value.PackageWeight = weight;
            Value.PackageCost = cost;
            Value.SalesPrice = price;
            Value.Material = MaterialText.Text.Trim();
            Value.IsMixed = IsMixedCheck.IsChecked == true;
            Value.IsFreezeDried = IsFreezeDriedCheck.IsChecked == true;
            Value.ProcessType = ProcessTypeText.Text.Trim();
            Value.Conclusion = ConclusionCombo.Text.Trim();
            Value.InnerItemsJson = InnerItemsJsonText.Text.Trim();
            Value.Remarks = RemarksText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static OverpackagingRecord Clone(OverpackagingRecord source)
        {
            return new OverpackagingRecord
            {
                Id = source.Id,
                TestId = source.TestId,
                TestDate = source.TestDate,
                ProductName = source.ProductName,
                BrandSeries = source.BrandSeries,
                ShapeType = source.ShapeType,
                Dimensions = source.Dimensions,
                PackageLayers = source.PackageLayers,
                PackageWeight = source.PackageWeight,
                PackageCost = source.PackageCost,
                SalesPrice = source.SalesPrice,
                Material = source.Material,
                IsMixed = source.IsMixed,
                IsFreezeDried = source.IsFreezeDried,
                ProcessType = source.ProcessType,
                Conclusion = source.Conclusion,
                Remarks = source.Remarks,
                InnerItemsJson = source.InnerItemsJson
            };
        }
    }
}
