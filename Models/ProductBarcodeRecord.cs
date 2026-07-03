using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 产品条码记录（product_barcodes）
    /// </summary>
    public class ProductBarcodeRecord
    {
        public long Id { get; set; }
        public string BarcodeId { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string BarcodeNumber { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string BrandSeries { get; set; } = string.Empty;
        public string PackageCategory { get; set; } = string.Empty;
        public string PackageSpec { get; set; } = string.Empty;
        public string NetContent { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public DateTime? GenerateDate { get; set; }
        public bool IsDisabled { get; set; }
        public string Remark { get; set; } = string.Empty;
    }
}
