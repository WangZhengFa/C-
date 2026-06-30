using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FoodEnterpriseIMS.Services;

namespace FoodEnterpriseIMS.Themes
{
    /// <summary>
    /// 主题配置读取工具，对应原 theme_config_helper
    /// 从数据库 system_config 读取主题/树/全局样式相关配置
    /// </summary>
    public static class ThemeConfigHelper
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// 读取所有主题相关配置
        /// </summary>
        public static (Dictionary<string, object> system, Dictionary<string, object> tree) ReadAllConfigs(DatabaseManager db)
        {
            var system = new Dictionary<string, object>();
            var tree = new Dictionary<string, object>();

            var rows = db.GetSystemConfigList("theme");
            foreach (var cfg in rows)
            {
                var key = cfg.TryGetValue("config_key", out var k) ? k?.ToString() ?? "" : "";
                var val = cfg.TryGetValue("config_value", out var v) ? v?.ToString() ?? "" : "";
                var type = cfg.TryGetValue("config_type", out var t) ? t?.ToString() ?? "string" : "string";

                object parsed = type.ToLower() switch
                {
                    "int" or "integer" => int.TryParse(val, out var iv) ? iv : 0,
                    "bool" or "boolean" => val is "1" or "true" or "yes" or "on",
                    "json" => JsonSerializer.Deserialize<Dictionary<string, object>>(val, _jsonOptions) ?? new Dictionary<string, object>(),
                    _ => val
                };

                system[key] = parsed;
            }

            var treeRows = db.GetSystemConfigList("tree_style");
            foreach (var cfg in treeRows)
            {
                var key = cfg.TryGetValue("config_key", out var k) ? k?.ToString() ?? "" : "";
                var val = cfg.TryGetValue("config_value", out var v) ? v?.ToString() ?? "" : "";
                tree[key] = val;
            }

            return (system, tree);
        }

        /// <summary>
        /// 读取字符串配置
        /// </summary>
        public static string CfgGet(DatabaseManager db, string configType, string key, string defaultValue = "")
        {
            var rows = db.GetSystemConfigList(configType);
            foreach (var cfg in rows)
            {
                if ((cfg.TryGetValue("config_key", out var k) ? k?.ToString() : null) == key)
                    return cfg.TryGetValue("config_value", out var v) ? v?.ToString() ?? defaultValue : defaultValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// 读取整数配置
        /// </summary>
        public static int CfgInt(DatabaseManager db, string configType, string key, int defaultValue = 0)
        {
            var s = CfgGet(db, configType, key, defaultValue.ToString());
            return int.TryParse(s, out var v) ? v : defaultValue;
        }

        /// <summary>
        /// 读取布尔配置
        /// </summary>
        public static bool CfgBool(DatabaseManager db, string configType, string key, bool defaultValue = false)
        {
            var s = CfgGet(db, configType, key, defaultValue ? "1" : "0").ToLower();
            return s is "1" or "true" or "yes" or "on";
        }

        /// <summary>
        /// 保存配置到数据库
        /// </summary>
        public static void SaveConfig(DatabaseManager db, string configType, string key, string value)
        {
            db.SetSystemConfig(configType, key, value);
        }
    }
}
