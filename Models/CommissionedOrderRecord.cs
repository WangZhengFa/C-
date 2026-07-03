using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 委托订单记录（order_forms）
    /// </summary>
    public class CommissionedOrderRecord
    {
        public long Id { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public DateTime? OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductSpec { get; set; } = string.Empty;
        public string OrderQuantity { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public string InspectionItems { get; set; } = string.Empty;
        public string InspectionStandard { get; set; } = string.Empty;
        public DateTime? RequiredDate { get; set; }
        public DateTime? ActualDate { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string InspectionFee { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
    }
}
