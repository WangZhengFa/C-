using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 自检数据记录（self_check_records）
    /// </summary>
    public class SelfCheckDataRecord
    {
        public long Id { get; set; }
        public string SelfCheckId { get; set; } = string.Empty;
        public DateTime? SamplingDate { get; set; }
        public DateTime? ReportDate { get; set; }
        public string InspectionId { get; set; } = string.Empty;
        public string SampleBatch { get; set; } = string.Empty;
        public string BrandSeries { get; set; } = string.Empty;
        public string SampleQuantity { get; set; } = string.Empty;
        public string RepresentativeQuantity { get; set; } = string.Empty;
        public string SampleSource { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public string NodeCode { get; set; } = string.Empty;
    }
}
