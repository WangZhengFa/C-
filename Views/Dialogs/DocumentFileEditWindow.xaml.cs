using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 编制文件编辑窗口
    /// </summary>
    public partial class DocumentFileEditWindow : Window
    {
        public DocumentFileRecord Value { get; private set; }

        public DocumentFileEditWindow(DocumentFileRecord? source, IEnumerable<DocumentFileRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new DocumentFileRecord { IsInvalid = false } : Clone(source);
            BindValue();
        }

        private void BindValue()
        {
            NodeCodeText.Text = Value.NodeCode;
            FileUniqueIdText.Text = Value.FileUniqueId;
            DepartmentText.Text = Value.Department;
            StdCategoryText.Text = Value.StdCategory;
            StdLevel1Text.Text = Value.StdLevel1;
            StdLevel2Text.Text = Value.StdLevel2;
            FileNameText.Text = Value.FileName;
            FileCodeText.Text = Value.FileCode;
            VersionText.Text = Value.Version;
            RevisionText.Text = Value.Revision;
            RevisionDatePicker.SelectedDate = Value.RevisionDate;
            EffectiveDatePicker.SelectedDate = Value.EffectiveDate;
            FileLinkText.Text = Value.FileLink;
            IsInvalidCheck.IsChecked = Value.IsInvalid;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var fileName = FileNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(fileName))
            {
                MessageBox.Show("文件名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.NodeCode = NodeCodeText.Text.Trim();
            Value.FileUniqueId = FileUniqueIdText.Text.Trim();
            Value.Department = DepartmentText.Text.Trim();
            Value.StdCategory = StdCategoryText.Text.Trim();
            Value.StdLevel1 = StdLevel1Text.Text.Trim();
            Value.StdLevel2 = StdLevel2Text.Text.Trim();
            Value.FileName = fileName;
            Value.FileCode = FileCodeText.Text.Trim();
            Value.Version = VersionText.Text.Trim();
            Value.Revision = RevisionText.Text.Trim();
            Value.RevisionDate = RevisionDatePicker.SelectedDate;
            Value.EffectiveDate = EffectiveDatePicker.SelectedDate;
            Value.FileLink = FileLinkText.Text.Trim();
            Value.IsInvalid = IsInvalidCheck.IsChecked == true;
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static DocumentFileRecord Clone(DocumentFileRecord source)
        {
            return new DocumentFileRecord
            {
                Id = source.Id,
                NodeCode = source.NodeCode,
                FileUniqueId = source.FileUniqueId,
                Department = source.Department,
                StdCategory = source.StdCategory,
                StdLevel1 = source.StdLevel1,
                StdLevel2 = source.StdLevel2,
                FileName = source.FileName,
                FileCode = source.FileCode,
                Version = source.Version,
                Revision = source.Revision,
                RevisionDate = source.RevisionDate,
                EffectiveDate = source.EffectiveDate,
                FileLink = source.FileLink,
                IsInvalid = source.IsInvalid,
                Remark = source.Remark,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };
        }
    }
}
