using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 质量监督整改实体
    /// </summary>
    public class QualitySupervision
    {
        public string SupervisionId { get; set; } = string.Empty;
        public DateTime DiscoveryDate { get; set; }
        public string ProjectCategory { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string NonCompliance { get; set; } = string.Empty;
        public string RectificationActions { get; set; } = string.Empty;
        public DateTime? RectificationDeadline { get; set; }
        public string RectificationResult { get; set; } = string.Empty;
        public string Supervisor { get; set; } = string.Empty;
        public bool IsReviewed { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }
}
