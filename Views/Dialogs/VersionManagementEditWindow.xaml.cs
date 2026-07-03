using System.Collections.Generic;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 版本日志编辑窗口
    /// </summary>
    public partial class VersionManagementEditWindow : Window
    {
        public VersionLogRecord Value { get; private set; }

        public VersionManagementEditWindow(VersionLogRecord? source, IEnumerable<VersionLogRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new VersionLogRecord() : Clone(source);
            BindValue();
        }

        private void BindValue()
        {
            VersionText.Text = Value.Version;
            UpdateDatePicker.SelectedDate = Value.UpdateDate;
            DescriptionText.Text = Value.Description;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var version = VersionText.Text.Trim();
            if (string.IsNullOrWhiteSpace(version))
            {
                MessageBox.Show(this, "版本号不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (UpdateDatePicker.SelectedDate == null)
            {
                MessageBox.Show(this, "更新日期不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.Version = version;
            Value.UpdateDate = UpdateDatePicker.SelectedDate;
            Value.Description = DescriptionText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static VersionLogRecord Clone(VersionLogRecord source)
        {
            return new VersionLogRecord
            {
                Id = source.Id,
                Version = source.Version,
                UpdateDate = source.UpdateDate,
                Description = source.Description
            };
        }
    }
}
