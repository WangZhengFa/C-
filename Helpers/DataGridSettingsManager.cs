using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;
using 食品信息管理系统.Views.Controls;

namespace FoodEnterpriseIMS.Helpers
{
    /// <summary>
    /// DataGrid 列设置管理器：加载、应用、打开设置对话框
    /// </summary>
    public sealed class DataGridSettingsManager
    {
        private readonly DataGrid _grid;
        private readonly string _group;
        private readonly bool _allowTableHeight;
        private readonly DataGridSettingsService _service = new();

        private DataGridSettings? _settings;

        public DataGridSettingsManager(DataGrid grid, string settingsGroup, bool allowTableHeight = false)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _group = string.IsNullOrWhiteSpace(settingsGroup) ? "default" : settingsGroup;
            _allowTableHeight = allowTableHeight;
        }

        /// <summary>
        /// 从数据库加载并应用设置（数据加载完成后调用）
        /// </summary>
        public void LoadAndApply()
        {
            _settings = _service.Load(_group);
            if (_settings == null)
            {
                _settings = DetectDefaults();
            }
            Apply(_settings);
        }

        /// <summary>
        /// 打开列设置对话框
        /// </summary>
        public void OpenSettingsDialog(Window owner)
        {
            if (_grid.Columns.Count == 0) return;

            var dialog = new Window
            {
                Title = $"{_group} - 字段设置",
                Width = 560,
                Height = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Owner = owner
            };

            var control = new DataGridSettingsControl();
            control.Initialize(_grid, _settings, _allowTableHeight);
            dialog.Content = control;

            control.SaveClicked += (_, _) =>
            {
                var newSettings = control.ToSettings(_group);
                Apply(newSettings);
                _service.Save(newSettings);
                _settings = newSettings;
                dialog.DialogResult = true;
                dialog.Close();
            };
            control.CancelClicked += (_, _) => dialog.Close();

            dialog.ShowDialog();
        }

        /// <summary>
        /// 应用设置到 DataGrid
        /// </summary>
        public void Apply(DataGridSettings settings)
        {
            if (settings == null) return;

            ApplyColumnOrder(settings.ColumnOrder);
            ApplyHiddenColumns(settings.HiddenColumns);
            ApplyColumnWidths(settings.WidthSettings, settings.FixedWidths);
            ApplyRowHeight(settings.RowHeight);
            ApplyFontSize(settings.FontSize);
            ApplyAlignments(settings.LeftColumns, settings.CenterColumns, settings.RightColumns);
            ApplySort(settings.SortSpecs);
            if (_allowTableHeight)
            {
                ApplyTableHeight(settings.TableHeightEnabled, settings.TableHeightMode, settings.TableHeight);
            }
        }

        private DataGridSettings DetectDefaults()
        {
            var settings = new DataGridSettings { SettingsGroup = _group };
            foreach (var col in _grid.Columns)
            {
                var header = col.Header?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(header)) continue;
                settings.ColumnOrder.Add(header);
                settings.WidthSettings[header] = "自适应";
            }
            settings.RowHeight = (int)(_grid.RowHeight > 0 ? _grid.RowHeight : 22);
            settings.FontSize = (int)(_grid.FontSize > 0 ? _grid.FontSize : 12);
            settings.TableHeight = Math.Max(120, (int)_grid.ActualHeight);
            return settings;
        }

        #region 应用具体项

        private void ApplyColumnOrder(List<string> order)
        {
            if (order == null || order.Count == 0) return;
            var headers = _grid.Columns.Select((c, i) => (Index: i, Header: c.Header?.ToString() ?? $"列{i}")).ToList();
            var ordered = order.Where(h => headers.Any(x => x.Header == h)).ToList();
            foreach (var h in headers.Select(x => x.Header))
            {
                if (!ordered.Contains(h)) ordered.Add(h);
            }

            for (int target = 0; target < ordered.Count; target++)
            {
                var current = headers.FindIndex(x => x.Header == ordered[target]);
                if (current < 0 || current == target) continue;
                _grid.Columns.Move(current, target);
                headers = _grid.Columns.Select((c, i) => (Index: i, Header: c.Header?.ToString() ?? $"列{i}")).ToList();
            }
        }

        private void ApplyHiddenColumns(List<string> hidden)
        {
            var hiddenSet = new HashSet<string>(hidden ?? new List<string>());
            foreach (var col in _grid.Columns)
            {
                var header = col.Header?.ToString() ?? string.Empty;
                col.Visibility = hiddenSet.Contains(header) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void ApplyColumnWidths(Dictionary<string, string> modes, Dictionary<string, int> fixedWidths)
        {
            if (modes == null) return;
            foreach (var col in _grid.Columns)
            {
                var header = col.Header?.ToString() ?? string.Empty;
                if (!modes.TryGetValue(header, out var mode)) continue;

                try
                {
                    switch (mode)
                    {
                        case "拉伸":
                            col.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                            break;
                        case "固定":
                        case "固定值":
                            var width = fixedWidths.TryGetValue(header, out var w) && w > 0 ? w : 120;
                            col.Width = new DataGridLength(width, DataGridLengthUnitType.Pixel);
                            break;
                        default:
                            col.Width = DataGridLength.Auto;
                            break;
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }

        private void ApplyRowHeight(int height)
        {
            if (height <= 0) return;
            _grid.RowHeight = height;
        }

        private void ApplyFontSize(int fontSize)
        {
            if (fontSize <= 0) return;
            _grid.FontSize = fontSize;

            // 表头也同步字体大小
            var style = new Style(typeof(DataGridColumnHeader));
            style.Setters.Add(new Setter(Control.FontSizeProperty, (double)fontSize));
            _grid.ColumnHeaderStyle = style;
        }

        private void ApplyAlignments(List<string> left, List<string> center, List<string> right)
        {
            var leftSet = new HashSet<string>(left ?? new List<string>());
            var centerSet = new HashSet<string>(center ?? new List<string>());
            var rightSet = new HashSet<string>(right ?? new List<string>());

            foreach (var col in _grid.Columns)
            {
                var header = col.Header?.ToString() ?? string.Empty;
                if (leftSet.Contains(header))
                    SetColumnAlignment(col, TextAlignment.Left, HorizontalAlignment.Left);
                else if (centerSet.Contains(header))
                    SetColumnAlignment(col, TextAlignment.Center, HorizontalAlignment.Center);
                else if (rightSet.Contains(header))
                    SetColumnAlignment(col, TextAlignment.Right, HorizontalAlignment.Right);
            }
        }

        private static void SetColumnAlignment(DataGridColumn column, TextAlignment textAlign, HorizontalAlignment horizontalAlign)
        {
            if (column is DataGridTextColumn textCol)
            {
                var style = new Style(typeof(TextBlock));
                style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, textAlign));
                textCol.ElementStyle = style;
            }
            else if (column is DataGridCheckBoxColumn checkCol)
            {
                var style = new Style(typeof(CheckBox));
                style.Setters.Add(new Setter(CheckBox.HorizontalAlignmentProperty, horizontalAlign));
                checkCol.ElementStyle = style;
            }
            else
            {
                var style = new Style(typeof(FrameworkElement));
                style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, horizontalAlign));
                column.CellStyle = style;
            }
        }

        private void ApplySort(List<(string ColumnName, bool Ascending)> sortSpecs)
        {
            if (sortSpecs == null || sortSpecs.Count == 0) return;
            if (_grid.ItemsSource == null) return;

            var view = CollectionViewSource.GetDefaultView(_grid.ItemsSource) as ListCollectionView
                ?? _grid.ItemsSource as ListCollectionView;
            if (view == null) return;

            using (view.DeferRefresh())
            {
                view.SortDescriptions.Clear();
                foreach (var (name, asc) in sortSpecs)
                {
                    var path = GetSortPropertyName(name);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        view.SortDescriptions.Add(new SortDescription(path, asc ? ListSortDirection.Ascending : ListSortDirection.Descending));
                    }
                }
            }

            // 显示排序指示器
            var header = _grid.Columns.FirstOrDefault(c => c.Header?.ToString() == sortSpecs[0].ColumnName);
            if (header != null)
            {
                _grid.Items.SortDescriptions.Clear();
                // WPF DataGrid 默认点击排序，这里仅做数据层排序
            }
        }

        private string? GetSortPropertyName(string header)
        {
            var col = _grid.Columns.FirstOrDefault(c => c.Header?.ToString() == header);
            if (col is DataGridBoundColumn bound && bound.Binding is Binding binding && !string.IsNullOrWhiteSpace(binding.Path?.Path))
            {
                return binding.Path.Path;
            }
            return null;
        }

        private void ApplyTableHeight(bool enabled, string mode, int height)
        {
            if (!enabled)
            {
                _grid.ClearValue(FrameworkElement.MaxHeightProperty);
                _grid.ClearValue(FrameworkElement.MinHeightProperty);
                return;
            }

            int target;
            if (mode == "自适应")
            {
                var headerHeight = _grid.ColumnHeaderHeight > 0 ? _grid.ColumnHeaderHeight : 28;
                var rowCount = _grid.Items.Count;
                var rowHeight = _grid.RowHeight > 0 ? _grid.RowHeight : 22;
                target = (int)(headerHeight + rowHeight * rowCount + 18);
                target = Math.Max(120, target);
            }
            else
            {
                target = Math.Max(120, height);
            }

            _grid.MaxHeight = target;
            _grid.MinHeight = target;
        }

        #endregion
    }
}
