using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 物料信息记录（material_infos）
    /// </summary>
    public class MaterialInfoRecord
    {
        public long Id { get; set; }
        public string MaterialId { get; set; } = string.Empty;
        public string NodeCode { get; set; } = string.Empty;
        public string FirstLevelCode { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public string PackagingSpec { get; set; } = string.Empty;
        public string BrandSeries { get; set; } = string.Empty;
        public int ExpiryDate { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Standard { get; set; } = string.Empty;
        public bool IsDisabled { get; set; }
        public string Remark { get; set; } = string.Empty;
    }
}
