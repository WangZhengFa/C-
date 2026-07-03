using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;
using 食品信息管理系统.Services;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 质量监督编辑窗口
    /// </summary>
    public partial class QualitySupervisionEditWindow : Window
    {
        /// <summary>
        /// 关闭请求事件，由主窗口处理
        /// </summary>
        public event EventHandler? CloseRequested;

        public bool Saved { get; private set; }

        private QualitySupervision _record;
        private readonly QualitySupervisionService _service;
        private string? _originalId;

        public QualitySupervisionEditWindow(QualitySupervision? record = null)
        {
            InitializeComponent();
            _service = new QualitySupervisionService();
            DataObject.AddPastingHandler(QuantityText, OnDecimalPaste);
            InitSelectors();
            _record = record ?? new QualitySupervision
            {
                DiscoveryDate = DateTime.Today
            };
            _originalId = !string.IsNullOrWhiteSpace(_record.SupervisionId) ? _record.SupervisionId : null;
            DataContext = _record;
        }

        private void InitSelectors()
        {
            ProjectCategoryCombo.Items.Clear();
            ProjectCategoryCombo.Items.Add("生产环境");
            ProjectCategoryCombo.Items.Add("原辅料管理");
            ProjectCategoryCombo.Items.Add("工艺流程");
            ProjectCategoryCombo.Items.Add("成品检验");
            ProjectCategoryCombo.Items.Add("标签标识");
            ProjectCategoryCombo.Items.Add("仓储运输");
            ProjectCategoryCombo.Items.Add("其他");

            RectificationResultCombo.Items.Clear();
            RectificationResultCombo.Items.Add("合格");
            RectificationResultCombo.Items.Add("不合格");
            RectificationResultCombo.Items.Add("整改中");

            SupervisorCombo.Items.Clear();
            foreach (var user in LocalSettingsService.RecentUsernames)
            {
                SupervisorCombo.Items.Add(user);
            }
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            _record = new QualitySupervision
            {
                DiscoveryDate = DateTime.Today
            };
            _originalId = null;
            DataContext = _record;
            Saved = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_record.ProjectName))
            {
                MessageBox.Show("请填写项目名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_record.RectificationDeadline.HasValue && _record.RectificationDeadline.Value.Date < _record.DiscoveryDate.Date)
            {
                MessageBox.Show("整改期限不能早于发现日期", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryValidateNonNegativeDecimal(_record.Quantity, "数量"))
            {
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(_record.SupervisionId) && _service.GetById(_record.SupervisionId) != null)
                {
                    _service.Update(_record);
                }
                else
                {
                    _service.Insert(_record);
                }
                Saved = true;
                MessageBox.Show("保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseRequested?.Invoke(this, EventArgs.Empty);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_originalId))
            {
                var restored = _service.GetById(_originalId);
                if (restored != null)
                {
                    _record = restored;
                    DataContext = _record;
                }
            }
            else
            {
                NewButton_Click(sender, e);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
            Close();
        }

        private static bool TryValidateNonNegativeDecimal(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            if (!decimal.TryParse(value.Trim(), out var parsed) || parsed < 0)
            {
                MessageBox.Show($"{fieldName}必须是大于等于0的数字", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void DecimalInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsDecimalFragment(e.Text);
        }

        private void OnDecimalPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var text = (e.DataObject.GetData(DataFormats.Text) as string)?.Trim();
            if (!string.IsNullOrWhiteSpace(text) && !TryParseNonNegativeDecimal(text))
            {
                e.CancelCommand();
            }
        }

        private static bool IsDecimalFragment(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            foreach (var ch in text)
            {
                if (!char.IsDigit(ch) && ch != '.' && ch != ',')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseNonNegativeDecimal(string text)
        {
            return decimal.TryParse(text, out var parsed) && parsed >= 0;
        }
    }
}
