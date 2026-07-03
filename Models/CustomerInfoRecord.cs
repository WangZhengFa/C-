namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 客户信息记录（customer_infos）
    /// </summary>
    public class CustomerInfoRecord
    {
        public long Id { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerType { get; set; } = string.Empty;
        public string LicenseNo { get; set; } = string.Empty;
        public string LicenseValidity { get; set; } = string.Empty;
        public string BusinessLicense { get; set; } = string.Empty;
        public string BusinessValidity { get; set; } = string.Empty;
        public string ContactAddress { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public bool IsDisabled { get; set; }
        public string Remark { get; set; } = string.Empty;
    }
}
