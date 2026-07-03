using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 工序生产数据记录（process_data_main）
    /// </summary>
    public class ProcessDataRecord
    {
        public long Id { get; set; }
        public string ProcessDataId { get; set; } = string.Empty;
        public string ProcessStepId { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public DateTime? ProductionDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Remark { get; set; } = string.Empty;
    }
}
