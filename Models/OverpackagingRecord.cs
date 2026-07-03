using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 过度包装检测记录（overpacking_records）
    /// </summary>
    public class OverpackagingRecord
    {
        public long Id { get; set; }
        public string TestId { get; set; } = string.Empty;
        public DateTime? TestDate { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string BrandSeries { get; set; } = string.Empty;
        public string ShapeType { get; set; } = string.Empty;
        public string Dimensions { get; set; } = string.Empty;
        public int PackageLayers { get; set; }
        public decimal PackageWeight { get; set; }
        public decimal PackageCost { get; set; }
        public decimal SalesPrice { get; set; }
        public string Material { get; set; } = string.Empty;
        public bool IsMixed { get; set; }
        public bool IsFreezeDried { get; set; }
        public string ProcessType { get; set; } = string.Empty;
        public string Conclusion { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string InnerItemsJson { get; set; } = string.Empty;
    }
}
