using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 标准规范记录（standard_specifications）
    /// </summary>
    public class StandardRegulationsRecord
    {
        public long Id { get; set; }
        public string StandardId { get; set; } = string.Empty;
        public string NodeCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Series { get; set; } = string.Empty;
        public string StandardName { get; set; } = string.Empty;
        public string StandardCode { get; set; } = string.Empty;
        public string PublishDept { get; set; } = string.Empty;
        public string PublishYear { get; set; } = string.Empty;
        public bool AppliesToHaccp { get; set; }
        public DateTime? PublishDate { get; set; }
        public DateTime? ImplementDate { get; set; }
        public DateTime? RevisionDate { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string StandardLink { get; set; } = string.Empty;
        public string NewStandardLink { get; set; } = string.Empty;
        public bool IsInvalid { get; set; }
        public string Remark { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsEnabled { get; set; }
    }
}
