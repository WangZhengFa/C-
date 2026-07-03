using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 标准规范编辑窗口
    /// </summary>
    public partial class StandardRegulationsEditWindow : Window
    {
        public StandardRegulationsRecord Value { get; private set; }

        public StandardRegulationsEditWindow(StandardRegulationsRecord? source, IEnumerable<StandardRegulationsRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new StandardRegulationsRecord { IsEnabled = true, SortOrder = 1 } : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<StandardRegulationsRecord> existing)
        {
            CategoryCombo.Items.Clear();
            SeriesCombo.Items.Clear();
            CategoryCombo.Items.Add("国家标准");
            CategoryCombo.Items.Add("行业标准");
            CategoryCombo.Items.Add("地方标准");
            CategoryCombo.Items.Add("企业标准");
            SeriesCombo.Items.Add("食品安全");
            SeriesCombo.Items.Add("通用");

            foreach (var value in existing.Select(x => x.Category).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!CategoryCombo.Items.Contains(value))
                {
                    CategoryCombo.Items.Add(value);
                }
            }

            foreach (var value in existing.Select(x => x.Series).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!SeriesCombo.Items.Contains(value))
                {
                    SeriesCombo.Items.Add(value);
                }
            }
        }

        private void BindValue()
        {
            StandardIdText.Text = Value.StandardId;
            NodeCodeText.Text = Value.NodeCode;
            CategoryCombo.Text = Value.Category;
            SeriesCombo.Text = Value.Series;
            StandardNameText.Text = Value.StandardName;
            StandardCodeText.Text = Value.StandardCode;
            PublishDeptText.Text = Value.PublishDept;
            PublishYearText.Text = Value.PublishYear;
            AppliesToHaccpCheck.IsChecked = Value.AppliesToHaccp;
            PublishDatePicker.SelectedDate = Value.PublishDate;
            ImplementDatePicker.SelectedDate = Value.ImplementDate;
            RevisionDatePicker.SelectedDate = Value.RevisionDate;
            EffectiveDatePicker.SelectedDate = Value.EffectiveDate;
            StandardLinkText.Text = Value.StandardLink;
            NewStandardLinkText.Text = Value.NewStandardLink;
            IsInvalidCheck.IsChecked = Value.IsInvalid;
            RemarkText.Text = Value.Remark;
            SortOrderText.Text = Value.SortOrder.ToString();
            IsEnabledCheck.IsChecked = Value.IsEnabled;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var standardId = StandardIdText.Text.Trim();
            var nodeCode = NodeCodeText.Text.Trim();
            var standardName = StandardNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(standardId))
            {
                MessageBox.Show(this, "标准编号不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(nodeCode))
            {
                MessageBox.Show(this, "节点编码不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(standardName))
            {
                MessageBox.Show(this, "标准名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(SortOrderText.Text) && !int.TryParse(SortOrderText.Text.Trim(), out var sortOrder))
            {
                MessageBox.Show(this, "排序必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.StandardId = standardId;
            Value.NodeCode = nodeCode;
            Value.Category = CategoryCombo.Text.Trim();
            Value.Series = SeriesCombo.Text.Trim();
            Value.StandardName = standardName;
            Value.StandardCode = StandardCodeText.Text.Trim();
            Value.PublishDept = PublishDeptText.Text.Trim();
            Value.PublishYear = PublishYearText.Text.Trim();
            Value.AppliesToHaccp = AppliesToHaccpCheck.IsChecked == true;
            Value.PublishDate = PublishDatePicker.SelectedDate;
            Value.ImplementDate = ImplementDatePicker.SelectedDate;
            Value.RevisionDate = RevisionDatePicker.SelectedDate;
            Value.EffectiveDate = EffectiveDatePicker.SelectedDate;
            Value.StandardLink = StandardLinkText.Text.Trim();
            Value.NewStandardLink = NewStandardLinkText.Text.Trim();
            Value.IsInvalid = IsInvalidCheck.IsChecked == true;
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

        private static StandardRegulationsRecord Clone(StandardRegulationsRecord source)
        {
            return new StandardRegulationsRecord
            {
                Id = source.Id,
                StandardId = source.StandardId,
                NodeCode = source.NodeCode,
                Category = source.Category,
                Series = source.Series,
                StandardName = source.StandardName,
                StandardCode = source.StandardCode,
                PublishDept = source.PublishDept,
                PublishYear = source.PublishYear,
                AppliesToHaccp = source.AppliesToHaccp,
                PublishDate = source.PublishDate,
                ImplementDate = source.ImplementDate,
                RevisionDate = source.RevisionDate,
                EffectiveDate = source.EffectiveDate,
                StandardLink = source.StandardLink,
                NewStandardLink = source.NewStandardLink,
                IsInvalid = source.IsInvalid,
                Remark = source.Remark,
                SortOrder = source.SortOrder,
                IsEnabled = source.IsEnabled
            };
        }
    }
}
