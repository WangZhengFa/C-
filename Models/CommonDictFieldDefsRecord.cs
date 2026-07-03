namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 通用字典字段定义记录（common_dict_field_defs）
    /// </summary>
    public class CommonDictFieldDefsRecord
    {
        public long Id { get; set; }
        public string FieldKey { get; set; } = string.Empty;
        public string FieldLabel { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public string Placeholder { get; set; } = string.Empty;
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public string Options { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsEnabled { get; set; }
        public string NodeCode { get; set; } = string.Empty;
    }
}
