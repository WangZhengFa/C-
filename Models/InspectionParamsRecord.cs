namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 检品参数记录（inspection_params）
    /// </summary>
    public class InspectionParamsRecord
    {
        public long Id { get; set; }
        public string InspectionId { get; set; } = string.Empty;
        public string NodeCode { get; set; } = string.Empty;
        public string InspectionName { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string Standard { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public bool IsDisabled { get; set; }
        public string Remark { get; set; } = string.Empty;
    }
}
