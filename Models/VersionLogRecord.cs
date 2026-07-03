using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 版本更新日志记录（version_log）
    /// </summary>
    public class VersionLogRecord
    {
        public long Id { get; set; }
        public string Version { get; set; } = string.Empty;
        public DateTime? UpdateDate { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
