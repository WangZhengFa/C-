namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 营养标签配置（nutrition_labels）
    /// </summary>
    public class NutritionLabelRecord
    {
        public string LabelId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DetectionMode { get; set; } = string.Empty;
        public bool HeavyMetal { get; set; }
        public string ClaimTypes { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public string NutrientData { get; set; } = string.Empty;
    }
}
