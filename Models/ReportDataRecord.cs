using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 报告数据记录（report_data_main）
    /// </summary>
    public class ReportDataRecord
    {
        public long Id { get; set; }
        public string NodeCode { get; set; } = string.Empty;
        public string ReportNumber { get; set; } = string.Empty;
        public string SampleName { get; set; } = string.Empty;
        public string SampleBatch { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string TestingInstitution { get; set; } = string.Empty;
        public DateTime? TestingDate { get; set; }
        public DateTime? ReportDate { get; set; }
        public string Conclusion { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
    }
}
