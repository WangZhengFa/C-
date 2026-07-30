using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FoodEnterpriseIMS.Helpers;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Controls
{
    /// <summary>
    /// 数据表格列设置控件
    /// </summary>
    public partial class DataGridSettingsControl : UserControl
    {
        private readonly DataGridSettingsEditorViewModel _vm = new();
        private DataGrid? _targetGrid;

        public event EventHandler? SaveClicked;
        public event EventHandler? CancelClicked;

        public DataGridSettingsControl()
        {
            InitializeComponent();
            DataContext = _vm;
        }

        /// <summary>
        /// 用目标 DataGrid 和当前设置初始化控件
        /// </summary>
        public void Initialize(DataGrid grid, DataGridSettings? settings, bool allowTableHeight)
        {
            _targetGrid = grid;
            _vm.AllowTableHeight = allowTableHeight;

            var headers = GetColumnHeaders(grid).ToList();

            // 显示与排序
            _vm.DisplayItems.Clear();
            foreach (var header in headers)
            {
                var hidden = settings?.HiddenColumns.Contains(header) == true;
                _vm.DisplayItems.Add(new DisplayOrderItem { Header = header, IsVisible = !hidden });
            }

            // 字段详细设置
            _vm.FieldRows.Clear();
            foreach (var header in headers)
            {
                var widthMode = NormalizeWidthMode(settings?.WidthSettings.GetValueOrDefault(header) ?? "自动");
                var fixedWidth = settings?.FixedWidths.GetValueOrDefault(header) ?? DetectFixedWidth(grid, header);

                var row = new ColumnSettingRow
                {
                    Header = header,
                    WidthMode = widthMode,
                    FixedWidth = fixedWidth,
                    IsLeft = settings?.LeftColumns.Contains(header) == true,
                    IsCenter = settings?.CenterColumns.Contains(header) == true,
                    IsRight = settings?.RightColumns.Contains(header) == true,
                    Sort = "不排"
                };

                var sort = settings?.SortSpecs?.FirstOrDefault(s => s.ColumnName == header);
                if (sort.HasValue)
                {
                    row.Sort = sort.Value.Ascending ? "升序" : "降序";
                }
                row.Sort = NormalizeSort(row.Sort);

                _vm.FieldRows.Add(row);
            }

            // 行数表高
            _vm.RowHeight = settings?.RowHeight > 0 ? settings.RowHeight : (int)(grid.RowHeight > 0 ? grid.RowHeight : 22);
            _vm.FontSize = settings?.FontSize > 0 ? settings.FontSize : (int)(grid.FontSize > 0 ? grid.FontSize : 12);
            _vm.MaxDisplayRows = settings?.MaxDisplayRows ?? 0;
            _vm.TableHeightEnabled = allowTableHeight && (settings?.TableHeightEnabled ?? false);
            _vm.TableHeightMode = settings?.TableHeightMode is "固定高度" or "自适应" ? settings.TableHeightMode : "固定高度";
            _vm.TableHeight = settings?.TableHeight > 0 ? settings.TableHeight : 300;
        }

        /// <summary>
        /// 收集当前设置
        /// </summary>
        public DataGridSettings ToSettings(string group)
        {
            var settings = new DataGridSettings { SettingsGroup = group };

            foreach (var item in _vm.DisplayItems)
            {
                settings.ColumnOrder.Add(item.Header);
                if (!item.IsVisible)
                    settings.HiddenColumns.Add(item.Header);
            }

            foreach (var row in _vm.FieldRows)
            {
                switch (row.Sort)
                {
                    case "升序":
                        settings.SortSpecs.Add((row.Header, true));
                        break;
                    case "降序":
                        settings.SortSpecs.Add((row.Header, false));
                        break;
                }

                // 旧数据兼容：遇到旧值时按新值保存
                var normalizedWidth = row.WidthMode switch
                {
                    "自适应" => "自动",
                    "固定值" => "固定",
                    _ => row.WidthMode
                };
                settings.WidthSettings[row.Header] = normalizedWidth;
                if (normalizedWidth == "固定")
                    settings.FixedWidths[row.Header] = row.FixedWidth;
            }

            settings.RowHeight = _vm.RowHeight;
            settings.FontSize = _vm.FontSize;
            settings.MaxDisplayRows = _vm.MaxDisplayRows;
            settings.TableHeightEnabled = _vm.TableHeightEnabled;
            settings.TableHeightMode = _vm.TableHeightMode;
            settings.TableHeight = _vm.TableHeight;

            return settings;
        }

        private static string NormalizeWidthMode(string mode)
        {
            return mode switch
            {
                "自适应" => "自动",
                "固定值" => "固定",
                _ => mode
            };
        }

        private static string NormalizeSort(string sort)
        {
            return sort switch
            {
                "不排序" => "不排",
                _ => sort
            };
        }

        private static IEnumerable<string> GetColumnHeaders(DataGrid grid)
        {
            for (int i = 0; i < grid.Columns.Count; i++)
            {
                yield return grid.Columns[i].Header?.ToString() ?? $"列{i}";
            }
        }

        private static string DetectWidthMode(DataGrid grid, string header)
        {
            var col = grid.Columns.FirstOrDefault(c => (c.Header?.ToString() ?? "") == header);
            if (col == null) return "自适应";
            return col.Width.UnitType switch
            {
                DataGridLengthUnitType.Star => "拉伸",
                DataGridLengthUnitType.Pixel => "固定值",
                _ => "自适应"
            };
        }

        private static int DetectFixedWidth(DataGrid grid, string header)
        {
            var col = grid.Columns.FirstOrDefault(c => (c.Header?.ToString() ?? "") == header);
            if (col == null) return 120;
            return col.Width.UnitType == DataGridLengthUnitType.Pixel ? (int)col.Width.Value : 120;
        }

        #region 显示与排序按钮
        private void TopButton_Click(object sender, RoutedEventArgs e) => MoveDisplayItem(int.MinValue);
        private void UpButton_Click(object sender, RoutedEventArgs e) => MoveDisplayItem(-1);
        private void DownButton_Click(object sender, RoutedEventArgs e) => MoveDisplayItem(1);
        private void BottomButton_Click(object sender, RoutedEventArgs e) => MoveDisplayItem(int.MaxValue);

        private void MoveDisplayItem(int delta)
        {
            var idx = DisplayList.SelectedIndex;
            if (idx < 0 || idx >= _vm.DisplayItems.Count) return;

            int target;
            if (delta == int.MinValue) target = 0;
            else if (delta == int.MaxValue) target = _vm.DisplayItems.Count - 1;
            else target = Math.Clamp(idx + delta, 0, _vm.DisplayItems.Count - 1);

            if (target == idx) return;
            var item = _vm.DisplayItems[idx];
            _vm.DisplayItems.RemoveAt(idx);
            _vm.DisplayItems.Insert(target, item);
            DisplayList.SelectedIndex = target;
        }
        #endregion

        #region 字段详细设置按钮
        private void FieldUpButton_Click(object sender, RoutedEventArgs e) => MoveFieldRow(-1);
        private void FieldDownButton_Click(object sender, RoutedEventArgs e) => MoveFieldRow(1);

        private void MoveFieldRow(int delta)
        {
            var idx = FieldGrid.SelectedIndex;
            if (idx < 0 || idx >= _vm.FieldRows.Count) return;

            var target = Math.Clamp(idx + delta, 0, _vm.FieldRows.Count - 1);
            if (target == idx) return;
            var item = _vm.FieldRows[idx];
            _vm.FieldRows.RemoveAt(idx);
            _vm.FieldRows.Insert(target, item);
            FieldGrid.SelectedIndex = target;
        }
        #endregion

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveClicked?.Invoke(this, EventArgs.Empty);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
