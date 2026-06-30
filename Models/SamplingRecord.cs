using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 取样记录实体
    /// </summary>
    public class SamplingRecord
    {
        public long Id { get; set; }
        public string SamplingId { get; set; } = string.Empty;
        public string NodeCode { get; set; } = string.Empty;
        public DateTime SamplingDate { get; set; }
        public DateTime InspectionDate { get; set; }
        public string SampleName { get; set; } = string.Empty;
        public string SampleBatch { get; set; } = string.Empty;
        public string SamplingQuantity { get; set; } = string.Empty;
        public string RepresentativeQuantity { get; set; } = string.Empty;
        public string SampleSource { get; set; } = string.Empty;
        public string Sampler { get; set; } = string.Empty;
        public string BrandSeries { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }
}
