using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 产品条码编辑窗口
    /// </summary>
    public partial class ProductBarcodeEditWindow : Window
    {
        public ProductBarcodeRecord Value { get; private set; }

        public ProductBarcodeEditWindow(ProductBarcodeRecord? source, IEnumerable<ProductBarcodeRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new ProductBarcodeRecord() : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<ProductBarcodeRecord> existing)
        {
            UnitCombo.Items.Clear();
            UnitCombo.Items.Add("g");
            UnitCombo.Items.Add("kg");
            UnitCombo.Items.Add("mL");
            UnitCombo.Items.Add("L");
            UnitCombo.Items.Add("个");
            UnitCombo.Items.Add("袋");
            UnitCombo.Items.Add("盒");

            foreach (var unit in existing.Select(x => x.Unit)
                                         .Where(x => !string.IsNullOrWhiteSpace(x))
                                         .Distinct()
                                         .OrderBy(x => x))
            {
                if (!UnitCombo.Items.Contains(unit))
                {
                    UnitCombo.Items.Add(unit);
                }
            }
        }

        private void BindValue()
        {
            BarcodeIdText.Text = Value.BarcodeId;
            CompanyCodeText.Text = Value.CompanyCode;
            BarcodeNumberText.Text = Value.BarcodeNumber;
            ProductIdText.Text = Value.ProductId;
            ProductNameText.Text = Value.ProductName;
            BrandSeriesText.Text = Value.BrandSeries;
            PackageCategoryText.Text = Value.PackageCategory;
            PackageSpecText.Text = Value.PackageSpec;
            NetContentText.Text = Value.NetContent;
            UnitCombo.Text = Value.Unit;
            GenerateDatePicker.SelectedDate = Value.GenerateDate;
            IsDisabledCheck.IsChecked = Value.IsDisabled;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var barcodeId = BarcodeIdText.Text.Trim();
            if (string.IsNullOrWhiteSpace(barcodeId))
            {
                MessageBox.Show(this, "条码ID不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var productName = ProductNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(productName))
            {
                MessageBox.Show(this, "产品名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.BarcodeId = barcodeId;
            Value.CompanyCode = CompanyCodeText.Text.Trim();
            Value.BarcodeNumber = BarcodeNumberText.Text.Trim();
            Value.ProductId = ProductIdText.Text.Trim();
            Value.ProductName = productName;
            Value.BrandSeries = BrandSeriesText.Text.Trim();
            Value.PackageCategory = PackageCategoryText.Text.Trim();
            Value.PackageSpec = PackageSpecText.Text.Trim();
            Value.NetContent = NetContentText.Text.Trim();
            Value.Unit = UnitCombo.Text.Trim();
            Value.GenerateDate = GenerateDatePicker.SelectedDate;
            Value.IsDisabled = IsDisabledCheck.IsChecked == true;
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static ProductBarcodeRecord Clone(ProductBarcodeRecord source)
        {
            return new ProductBarcodeRecord
            {
                Id = source.Id,
                BarcodeId = source.BarcodeId,
                CompanyCode = source.CompanyCode,
                BarcodeNumber = source.BarcodeNumber,
                ProductId = source.ProductId,
                ProductName = source.ProductName,
                BrandSeries = source.BrandSeries,
                PackageCategory = source.PackageCategory,
                PackageSpec = source.PackageSpec,
                NetContent = source.NetContent,
                Unit = source.Unit,
                GenerateDate = source.GenerateDate,
                IsDisabled = source.IsDisabled,
                Remark = source.Remark
            };
        }
    }
}
