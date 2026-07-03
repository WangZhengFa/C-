namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 更新管理设置项
    /// </summary>
    public class UpdateManagementSettingRecord
    {
        public string ConfigKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
