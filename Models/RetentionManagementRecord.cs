using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 留样管理记录（sample_retention_records）
    /// </summary>
    public class RetentionManagementRecord
    {
        public long Id { get; set; }
        public string RetentionCode { get; set; } = string.Empty;
        public string ReportCode { get; set; } = string.Empty;
        public string MaterialId { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? RetentionDate { get; set; }
        public string RetentionPerson { get; set; } = string.Empty;
        public DateTime? RetentionDeadline { get; set; }
        public string RetentionLocation { get; set; } = string.Empty;
        public int RetentionQuantity { get; set; }
        public string StorageCondition { get; set; } = string.Empty;
        public string SampleStatus { get; set; } = string.Empty;
        public DateTime? DisposeDate { get; set; }
        public string DisposePerson { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
    }
}
