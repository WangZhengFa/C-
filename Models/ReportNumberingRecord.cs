using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 报告编号记录（report_numbers）
    /// </summary>
    public class ReportNumberingRecord
    {
        public long Id { get; set; }
        public string ReportCode { get; set; } = string.Empty;
        public DateTime? ProductionDate { get; set; }
        public DateTime? InspectionDate { get; set; }
        public DateTime? ReportDate { get; set; }
        public string MaterialId { get; set; } = string.Empty;
        public string SampleBatch { get; set; } = string.Empty;
        public string SampleQuantity { get; set; } = string.Empty;
        public string BatchQuantity { get; set; } = string.Empty;
        public string SampleSource { get; set; } = string.Empty;
        public string ReportResult { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public string NodeCode { get; set; } = string.Empty;
    }
}
