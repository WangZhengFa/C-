using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 产品信息编辑窗口
    /// </summary>
    public partial class ProductInfoEditWindow : Window
    {
        public ProductInfoRecord Value { get; private set; }

        public ProductInfoEditWindow(ProductInfoRecord? source, IEnumerable<ProductInfoRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new ProductInfoRecord { IsEnabled = true } : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<ProductInfoRecord> existing)
        {
            FoodCategoryCombo.Items.Clear();
            DosageFormCombo.Items.Clear();
            OwnershipStatusCombo.Items.Clear();
            ApprovalMethodCombo.Items.Clear();

            FoodCategoryCombo.Items.Add("预包装食品");
            FoodCategoryCombo.Items.Add("散装食品");
            DosageFormCombo.Items.Add("固体");
            DosageFormCombo.Items.Add("液体");
            DosageFormCombo.Items.Add("半固体");
            OwnershipStatusCombo.Items.Add("自有");
            OwnershipStatusCombo.Items.Add("委托");
            OwnershipStatusCombo.Items.Add("外购");
            ApprovalMethodCombo.Items.Add("备案");
            ApprovalMethodCombo.Items.Add("审批");

            foreach (var value in existing.Select(x => x.FoodCategory).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!FoodCategoryCombo.Items.Contains(value))
                {
                    FoodCategoryCombo.Items.Add(value);
                }
            }

            foreach (var value in existing.Select(x => x.DosageForm).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!DosageFormCombo.Items.Contains(value))
                {
                    DosageFormCombo.Items.Add(value);
                }
            }

            foreach (var value in existing.Select(x => x.OwnershipStatus).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!OwnershipStatusCombo.Items.Contains(value))
                {
                    OwnershipStatusCombo.Items.Add(value);
                }
            }

            foreach (var value in existing.Select(x => x.ApprovalMethod).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!ApprovalMethodCombo.Items.Contains(value))
                {
                    ApprovalMethodCombo.Items.Add(value);
                }
            }
        }

        private void BindValue()
        {
            NodeCodeText.Text = Value.NodeCode;
            ProductIdText.Text = Value.ProductId;
            ProductNameText.Text = Value.ProductName;
            ProductCodeText.Text = Value.ProductCode;
            StandardCodeText.Text = Value.StandardCode;
            FoodCategoryCombo.Text = Value.FoodCategory;
            DosageFormCombo.Text = Value.DosageForm;
            OwnershipStatusCombo.Text = Value.OwnershipStatus;
            ApprovalMethodCombo.Text = Value.ApprovalMethod;
            ApprovalDepartmentText.Text = Value.ApprovalDepartment;
            ApprovalDatePicker.SelectedDate = Value.ApprovalDate;
            StandardValidityText.Text = Value.StandardValidity;
            EnterpriseCodeText.Text = Value.EnterpriseCode;
            EnterpriseYearText.Text = Value.EnterpriseYear <= 0 ? string.Empty : Value.EnterpriseYear.ToString();
            EnterpriseEffectiveDatePicker.SelectedDate = Value.EnterpriseEffectiveDate;
            StandardLinkText.Text = Value.StandardLink;
            EnterpriseLinkText.Text = Value.EnterpriseLink;
            RemarkText.Text = Value.Remark;
            SortOrderText.Text = Value.SortOrder.ToString();
            IsEnabledCheck.IsChecked = Value.IsEnabled;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var productName = ProductNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(productName))
            {
                MessageBox.Show(this, "产品名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(EnterpriseYearText.Text) && !int.TryParse(EnterpriseYearText.Text.Trim(), out var enterpriseYear))
            {
                MessageBox.Show(this, "企业年限必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse((SortOrderText.Text ?? string.Empty).Trim(), out var sortOrder))
            {
                MessageBox.Show(this, "排序号必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.NodeCode = NodeCodeText.Text.Trim();
            Value.ProductId = ProductIdText.Text.Trim();
            Value.ProductName = productName;
            Value.ProductCode = ProductCodeText.Text.Trim();
            Value.StandardCode = StandardCodeText.Text.Trim();
            Value.FoodCategory = FoodCategoryCombo.Text.Trim();
            Value.DosageForm = DosageFormCombo.Text.Trim();
            Value.OwnershipStatus = OwnershipStatusCombo.Text.Trim();
            Value.ApprovalMethod = ApprovalMethodCombo.Text.Trim();
            Value.ApprovalDepartment = ApprovalDepartmentText.Text.Trim();
            Value.ApprovalDate = ApprovalDatePicker.SelectedDate;
            Value.StandardValidity = StandardValidityText.Text.Trim();
            Value.EnterpriseCode = EnterpriseCodeText.Text.Trim();
            Value.EnterpriseYear = string.IsNullOrWhiteSpace(EnterpriseYearText.Text) ? 0 : int.Parse(EnterpriseYearText.Text.Trim());
            Value.EnterpriseEffectiveDate = EnterpriseEffectiveDatePicker.SelectedDate;
            Value.StandardLink = StandardLinkText.Text.Trim();
            Value.EnterpriseLink = EnterpriseLinkText.Text.Trim();
            Value.Remark = RemarkText.Text.Trim();
            Value.SortOrder = sortOrder;
            Value.IsEnabled = IsEnabledCheck.IsChecked == true;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static ProductInfoRecord Clone(ProductInfoRecord source)
        {
            return new ProductInfoRecord
            {
                Id = source.Id,
                NodeCode = source.NodeCode,
                ProductId = source.ProductId,
                ProductName = source.ProductName,
                ProductCode = source.ProductCode,
                StandardCode = source.StandardCode,
                FoodCategory = source.FoodCategory,
                DosageForm = source.DosageForm,
                OwnershipStatus = source.OwnershipStatus,
                ApprovalMethod = source.ApprovalMethod,
                ApprovalDepartment = source.ApprovalDepartment,
                ApprovalDate = source.ApprovalDate,
                StandardValidity = source.StandardValidity,
                EnterpriseCode = source.EnterpriseCode,
                EnterpriseYear = source.EnterpriseYear,
                EnterpriseEffectiveDate = source.EnterpriseEffectiveDate,
                StandardLink = source.StandardLink,
                EnterpriseLink = source.EnterpriseLink,
                Remark = source.Remark,
                SortOrder = source.SortOrder,
                IsEnabled = source.IsEnabled
            };
        }
    }
}
