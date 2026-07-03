using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 外部抽检记录
    /// </summary>
    public class ExternalSamplingRecord
    {
        public long Id { get; set; }
        public string SamplingId { get; set; } = string.Empty;
        public DateTime? SamplingDate { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public string ProductQuantity { get; set; } = string.Empty;
        public string SamplingQuantity { get; set; } = string.Empty;
        public string SamplingPrice { get; set; } = string.Empty;
        public string MonitorType { get; set; } = string.Empty;
        public string SamplingOrg { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
    }
}
