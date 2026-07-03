namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 工序参数记录（process_params_main）
    /// </summary>
    public class ProcessParamsRecord
    {
        public long Id { get; set; }
        public string ProcessStepId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public bool IsDisabled { get; set; }
        public string Remark { get; set; } = string.Empty;
    }
}
