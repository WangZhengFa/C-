using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 产品信息记录（product_infos）
    /// </summary>
    public class ProductInfoRecord
    {
        public long Id { get; set; }
        public string NodeCode { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string StandardCode { get; set; } = string.Empty;
        public string FoodCategory { get; set; } = string.Empty;
        public string DosageForm { get; set; } = string.Empty;
        public string OwnershipStatus { get; set; } = string.Empty;
        public string ApprovalMethod { get; set; } = string.Empty;
        public string ApprovalDepartment { get; set; } = string.Empty;
        public DateTime? ApprovalDate { get; set; }
        public string StandardValidity { get; set; } = string.Empty;
        public string EnterpriseCode { get; set; } = string.Empty;
        public int EnterpriseYear { get; set; }
        public DateTime? EnterpriseEffectiveDate { get; set; }
        public string StandardLink { get; set; } = string.Empty;
        public string EnterpriseLink { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsEnabled { get; set; }
    }
}
