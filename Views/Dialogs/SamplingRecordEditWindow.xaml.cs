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
    /// 取样记录编辑窗口
    /// </summary>
    public partial class SamplingRecordEditWindow : Window
    {
        /// <summary>
        /// 关闭请求事件，由主窗口处理
        /// </summary>
        public event EventHandler? CloseRequested;

        public bool Saved { get; private set; }

        private SamplingRecord _record;
        private readonly SamplingRecordService _service;
        private long? _originalId;

        public SamplingRecordEditWindow(SamplingRecord? record = null)
        {
            InitializeComponent();
            _service = new SamplingRecordService();
            DataObject.AddPastingHandler(SamplingQuantityText, OnDecimalPaste);
            DataObject.AddPastingHandler(RepresentativeQuantityText, OnDecimalPaste);
            InitSelectors();
            _record = record ?? new SamplingRecord
            {
                SamplingDate = DateTime.Today,
                InspectionDate = DateTime.Today
            };
            _originalId = _record.Id > 0 ? _record.Id : null;
            DataContext = _record;
        }

        private void InitSelectors()
        {
            SampleSourceCombo.Items.Clear();
            SampleSourceCombo.Items.Add("生产企业");
            SampleSourceCombo.Items.Add("流通环节");
            SampleSourceCombo.Items.Add("餐饮环节");
            SampleSourceCombo.Items.Add("网络抽样");
            SampleSourceCombo.Items.Add("其他");

            SamplerCombo.Items.Clear();
            foreach (var user in LocalSettingsService.RecentUsernames)
            {
                SamplerCombo.Items.Add(user);
            }
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            _record = new SamplingRecord
            {
                SamplingDate = DateTime.Today,
                InspectionDate = DateTime.Today
            };
            _originalId = null;
            DataContext = _record;
            Saved = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_record.SampleName))
            {
                MessageBox.Show("请填写检品名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(_record.NodeCode))
            {
                MessageBox.Show("请填写节点编号", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_record.InspectionDate < _record.SamplingDate)
            {
                MessageBox.Show("检验日期不能早于取样日期", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryValidateNonNegativeDecimal(_record.SamplingQuantity, "取样量"))
            {
                return;
            }

            if (!TryValidateNonNegativeDecimal(_record.RepresentativeQuantity, "代表量"))
            {
                return;
            }

            try
            {
                if (_record.Id > 0)
                {
                    _service.Update(_record);
                }
                else
                {
                    var newId = _service.Insert(_record);
                    _record.Id = newId;
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
            if (_originalId.HasValue)
            {
                var restored = _service.GetById(_originalId.Value);
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
