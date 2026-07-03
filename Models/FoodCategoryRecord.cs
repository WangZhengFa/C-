namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 食品分类记录（food_categories）
    /// </summary>
    public class FoodCategoryRecord
    {
        public long Id { get; set; }
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ParentCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsEnabled { get; set; }
    }
}
