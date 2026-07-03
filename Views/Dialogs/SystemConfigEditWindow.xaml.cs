using System.Collections.Generic;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 系统配置编辑窗口
    /// </summary>
    public partial class SystemConfigEditWindow : Window
    {
        public SystemConfigRecord Value { get; private set; }

        public SystemConfigEditWindow(SystemConfigRecord? source, IEnumerable<SystemConfigRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new SystemConfigRecord() : Clone(source);
            BindValue();
        }

        private void BindValue()
        {
            ConfigKeyText.Text = Value.ConfigKey;
            ValueText.Text = Value.Value;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var configKey = ConfigKeyText.Text.Trim();
            if (string.IsNullOrWhiteSpace(configKey))
            {
                MessageBox.Show(this, "配置键不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.ConfigKey = configKey;
            Value.Value = ValueText.Text;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static SystemConfigRecord Clone(SystemConfigRecord source)
        {
            return new SystemConfigRecord
            {
                ConfigKey = source.ConfigKey,
                Value = source.Value
            };
        }
    }
}
