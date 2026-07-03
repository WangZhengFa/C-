using System.Collections.Generic;
using System.Linq;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 加盖公章设置服务
    /// </summary>
    public class SealStampService
    {
        private const string ConfigType = "seal_stamp";

        private static readonly List<SealStampSettingRecord> Defaults = new()
        {
            new SealStampSettingRecord { ConfigKey = "enabled", DisplayName = "启用公章", Description = "1 表示在打印或导出时启用公章叠加" },
            new SealStampSettingRecord { ConfigKey = "image_path", DisplayName = "公章图片", Description = "公章图片文件路径" },
            new SealStampSettingRecord { ConfigKey = "opacity", DisplayName = "透明度", Description = "0 到 1 之间的浮点数" },
            new SealStampSettingRecord { ConfigKey = "offset_x", DisplayName = "水平偏移", Description = "公章叠加时的水平偏移像素" },
            new SealStampSettingRecord { ConfigKey = "offset_y", DisplayName = "垂直偏移", Description = "公章叠加时的垂直偏移像素" }
        };

        public List<SealStampSettingRecord> ListAll(DatabaseManager db)
        {
            var rows = db.GetSystemConfigList(ConfigType);
            var result = Defaults.Select(x => new SealStampSettingRecord
            {
                ConfigKey = x.ConfigKey,
                DisplayName = x.DisplayName,
                Description = x.Description,
                Value = string.Empty
            }).ToList();

            foreach (var row in rows)
            {
                var key = row.TryGetValue("config_key", out var k) ? k?.ToString() ?? string.Empty : string.Empty;
                var value = row.TryGetValue("config_value", out var v) ? v?.ToString() ?? string.Empty : string.Empty;

                var existing = result.FirstOrDefault(x => string.Equals(x.ConfigKey, key, System.StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.Value = value;
                }
                else if (!string.IsNullOrWhiteSpace(key))
                {
                    result.Add(new SealStampSettingRecord
                    {
                        ConfigKey = key,
                        DisplayName = key,
                        Value = value,
                        Description = string.Empty
                    });
                }
            }

            return result;
        }

        public void SaveAll(DatabaseManager db, IEnumerable<SealStampSettingRecord> records)
        {
            foreach (var record in records)
            {
                var key = (record.ConfigKey ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                db.SetSystemConfig(ConfigType, key, record.Value ?? string.Empty);
            }
        }
    }
}
