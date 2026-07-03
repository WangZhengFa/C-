using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 样品分发记录（sample_receive_send）
    /// </summary>
    public class SampleDistributionRecord
    {
        public long Id { get; set; }
        public string ReceiveSendId { get; set; } = string.Empty;
        public DateTime? ReceiveSendDate { get; set; }
        public DateTime? InspectionDate { get; set; }
        public DateTime? ReportDate { get; set; }
        public string SampleName { get; set; } = string.Empty;
        public string SampleBatch { get; set; } = string.Empty;
        public string SampleQuantity { get; set; } = string.Empty;
        public string RetentionQuantity { get; set; } = string.Empty;
        public string RepresentativeQuantity { get; set; } = string.Empty;
        public string SampleSource { get; set; } = string.Empty;
        public bool IsReinspection { get; set; }
        public string Remark { get; set; } = string.Empty;
        public string NodeCode { get; set; } = string.Empty;
    }
}
