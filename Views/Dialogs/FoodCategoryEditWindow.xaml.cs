using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 食品分类编辑窗口
    /// </summary>
    public partial class FoodCategoryEditWindow : Window
    {
        public FoodCategoryRecord Value { get; private set; }

        public FoodCategoryEditWindow(FoodCategoryRecord? source, IEnumerable<FoodCategoryRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new FoodCategoryRecord { IsEnabled = true, SortOrder = 0 } : Clone(source);
            BindValue();
        }

        private void BindValue()
        {
            CategoryCodeText.Text = Value.CategoryCode;
            CategoryNameText.Text = Value.CategoryName;
            ParentCodeText.Text = Value.ParentCode;
            SortOrderText.Text = Value.SortOrder.ToString(CultureInfo.InvariantCulture);
            DescriptionText.Text = Value.Description;
            IsEnabledCheck.IsChecked = Value.IsEnabled;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var categoryCode = CategoryCodeText.Text.Trim();
            var categoryName = CategoryNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(categoryCode))
            {
                MessageBox.Show("分类编码不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                MessageBox.Show("分类名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(SortOrderText.Text) && !int.TryParse(SortOrderText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sortOrder))
            {
                MessageBox.Show("排序必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.CategoryCode = categoryCode;
            Value.CategoryName = categoryName;
            Value.ParentCode = ParentCodeText.Text.Trim();
            Value.SortOrder = string.IsNullOrWhiteSpace(SortOrderText.Text) ? 0 : int.Parse(SortOrderText.Text.Trim(), CultureInfo.InvariantCulture);
            Value.Description = DescriptionText.Text.Trim();
            Value.IsEnabled = IsEnabledCheck.IsChecked == true;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static FoodCategoryRecord Clone(FoodCategoryRecord source)
        {
            return new FoodCategoryRecord
            {
                Id = source.Id,
                CategoryCode = source.CategoryCode,
                CategoryName = source.CategoryName,
                ParentCode = source.ParentCode,
                Description = source.Description,
                SortOrder = source.SortOrder,
                IsEnabled = source.IsEnabled
            };
        }
    }
}
