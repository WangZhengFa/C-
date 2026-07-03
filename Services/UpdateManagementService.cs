using System.Collections.Generic;
using System.Linq;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 更新管理服务
    /// </summary>
    public class UpdateManagementService
    {
        private const string ConfigType = "update_management";

        private static readonly List<UpdateManagementSettingRecord> Defaults = new()
        {
            new UpdateManagementSettingRecord { ConfigKey = "latest_version", DisplayName = "最新版本号", Description = "用于提示客户端可更新到的版本号" },
            new UpdateManagementSettingRecord { ConfigKey = "download_url", DisplayName = "下载地址", Description = "更新包下载链接或发布页链接" },
            new UpdateManagementSettingRecord { ConfigKey = "force_update", DisplayName = "强制更新", Description = "1 表示必须更新，0 表示可跳过" },
            new UpdateManagementSettingRecord { ConfigKey = "release_notes", DisplayName = "更新说明", Description = "更新内容摘要" }
        };

        public List<UpdateManagementSettingRecord> ListAll(DatabaseManager db)
        {
            var rows = db.GetSystemConfigList(ConfigType);
            var result = Defaults.Select(x => new UpdateManagementSettingRecord
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
                    result.Add(new UpdateManagementSettingRecord
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

        public void SaveAll(DatabaseManager db, IEnumerable<UpdateManagementSettingRecord> records)
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
