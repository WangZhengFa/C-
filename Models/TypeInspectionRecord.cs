using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 型式检验记录
    /// </summary>
    public class TypeInspectionRecord
    {
        public long Id { get; set; }
        public string InspectionId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public DateTime? SendDate { get; set; }
        public DateTime? ReportDate { get; set; }
        public string Conclusion { get; set; } = "合格";
        public string TestingOrg { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
    }
}
