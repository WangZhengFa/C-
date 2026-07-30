using System.Collections.Generic;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 数据表格列设置持久化模型
    /// </summary>
    public class DataGridSettings
    {
        public string SettingsGroup { get; set; } = string.Empty;

        /// <summary>隐藏列标题集合</summary>
        public List<string> HiddenColumns { get; set; } = new();

        /// <summary>列显示顺序（标题）</summary>
        public List<string> ColumnOrder { get; set; } = new();

        /// <summary>列宽模式：自适应/拉伸/固定值</summary>
        public Dictionary<string, string> WidthSettings { get; set; } = new();

        /// <summary>固定宽度值</summary>
        public Dictionary<string, int> FixedWidths { get; set; } = new();

        /// <summary>行高</summary>
        public int RowHeight { get; set; } = 22;

        /// <summary>字体大小</summary>
        public int FontSize { get; set; } = 12;

        /// <summary>是否启用表格高度设置</summary>
        public bool TableHeightEnabled { get; set; }

        /// <summary>表格高度模式：固定高度/自适应</summary>
        public string TableHeightMode { get; set; } = string.Empty;

        /// <summary>表格高度</summary>
        public int TableHeight { get; set; } = 300;

        /// <summary>最大显示行数（0 表示不限制）</summary>
        public int MaxDisplayRows { get; set; }

        /// <summary>左对齐列</summary>
        public List<string> LeftColumns { get; set; } = new();

        /// <summary>居中对齐列</summary>
        public List<string> CenterColumns { get; set; } = new();

        /// <summary>右对齐列</summary>
        public List<string> RightColumns { get; set; } = new();

        /// <summary>排序规格：列标题 + 是否升序</summary>
        public List<(string ColumnName, bool Ascending)> SortSpecs { get; set; } = new();
    }
}
