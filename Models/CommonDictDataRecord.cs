using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 通用字典数据记录（common_dict_data）
    /// </summary>
    public class CommonDictDataRecord
    {
        public long Id { get; set; }
        public string NodeCode { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Field1 { get; set; } = string.Empty;
        public string Field2 { get; set; } = string.Empty;
        public string Field3 { get; set; } = string.Empty;
        public string Field4 { get; set; } = string.Empty;
        public string Field5 { get; set; } = string.Empty;
        public decimal? Number1 { get; set; }
        public decimal? Number2 { get; set; }
        public DateTime? Date1 { get; set; }
        public DateTime? Date2 { get; set; }
        public bool Flag1 { get; set; }
        public bool Flag2 { get; set; }
        public decimal? Amount { get; set; }
        public string Remark { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsEnabled { get; set; }
    }
}
