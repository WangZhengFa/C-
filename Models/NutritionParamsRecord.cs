using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 营养参数记录（nutrient_parameters）
    /// </summary>
    public class NutritionParamsRecord
    {
        public long Id { get; set; }
        public int SortOrder { get; set; }
        public string Nutrient { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal? RoundingInterval { get; set; }
        public decimal? ZeroThreshold { get; set; }
        public decimal? EnergyValue { get; set; }
        public decimal? LowerError { get; set; }
        public decimal? UpperError { get; set; }
        public decimal? Nrv { get; set; }
        public bool Core { get; set; }
        public bool HeavyMetal { get; set; }
        public bool IsSportsNutrition { get; set; }
        public string DailyUsageRange { get; set; } = string.Empty;
        public bool Disabled { get; set; }
        public string ParamKey { get; set; } = string.Empty;
        public string ParamValue { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
    }
}
