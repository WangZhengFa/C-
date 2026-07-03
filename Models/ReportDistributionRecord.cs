using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 报告发放记录
    /// </summary>
    public class ReportDistributionRecord
    {
        public long Id { get; set; }
        public string ReportCode { get; set; } = string.Empty;
        public DateTime? DistributionDate { get; set; }
        public string Distributor { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
        public DateTime? ReceiveDate { get; set; }
        public string Acceptor { get; set; } = string.Empty;
        public bool IsReceived { get; set; }
        public DateTime? AcceptDate { get; set; }
        public bool IsAccepted { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }
}
