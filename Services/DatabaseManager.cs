using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 数据库管理（简化版）
    /// </summary>
    public class DatabaseManager
    {
        private readonly string _dbPath;

        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");

        private static void WriteLog(string message)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFilePath, logEntry);
            }
            catch
            {
                // 忽略日志写入错误
            }
        }

        public DatabaseManager(string dbPath)
        {
            _dbPath = dbPath ?? "FoodEnterpriseIMS.db";
        }

        public bool CheckConnection()
        {
            try
            {
                var cfg = FoodEnterpriseIMS.Database.MysqlDbInitializer.LoadMysqlConfig();
                var connStr = $"server={cfg.Host};port={cfg.Port};user={cfg.User};password={cfg.Password};database={cfg.Database};charset=utf8mb4;Pooling=true;Max Pool Size=10;Min Pool Size=1;Connection Timeout=5";

                using var conn = new MySqlConnection(connStr);
                conn.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Reconnect()
        {
            // TODO: 实现数据库重连
        }

        public string? GetLatestVersion()
        {
            try
            {
                var cfg = FoodEnterpriseIMS.Database.MysqlDbInitializer.LoadMysqlConfig();
                var connStr = $"server={cfg.Host};port={cfg.Port};user={cfg.User};password={cfg.Password};database={cfg.Database};charset=utf8mb4;Pooling=true;Max Pool Size=10;Min Pool Size=1";

                using var conn = new MySqlConnection(connStr);
                conn.Open();

                const string sql = "SELECT `value` FROM system_config WHERE `key` = 'app_version' LIMIT 1";
                using var cmd = new MySqlCommand(sql, conn);
                var result = cmd.ExecuteScalar();
                return result?.ToString();
            }
            catch (Exception ex)
            {
                WriteLog($"[GetLatestVersion] 读取版本号失败: {ex.Message}");
                return null;
            }
        }

        public List<Dictionary<string, object>> GetMenuList()
        {
            var menuList = new List<Dictionary<string, object>>();
            
            try
            {
                // 从数据库 tree_nodes 表读取 system_menu 的全部节点
                var cfg = FoodEnterpriseIMS.Database.MysqlDbInitializer.LoadMysqlConfig();

                var connStr = $"server={cfg.Host};port={cfg.Port};user={cfg.User};password={cfg.Password};database={cfg.Database};charset=utf8mb4;Pooling=true;Max Pool Size=10;Min Pool Size=1";

                using var conn = new MySqlConnection(connStr);
                conn.Open();
                
                var sql = @"
                    SELECT node_code, parent_code, title, sort_order, payload_json
                    FROM tree_nodes
                    WHERE tree_key = 'system_menu'
                    ORDER BY sort_order ASC";
                
                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                
                while (reader.Read())
                {
                    var nodeCode = reader.GetString("node_code");
                    var parentCode = reader.IsDBNull(reader.GetOrdinal("parent_code")) ? "" : reader.GetString("parent_code");
                    var title = reader.GetString("title");
                    var sortOrder = reader.GetInt32("sort_order");
                    var payloadJson = reader.IsDBNull(reader.GetOrdinal("payload_json")) ? null : reader.GetString("payload_json");

                    var menuItem = new Dictionary<string, object>
                    {
                        ["menu_key"] = nodeCode,
                        ["title"] = title,
                        ["parent_key"] = parentCode,
                        ["sort_order"] = sortOrder
                    };
                    
                    // 解析 payload_json 获取 component_path、csharp_class 等信息
                    if (!string.IsNullOrEmpty(payloadJson))
                    {
                        try
                        {
                            var payload = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(payloadJson);
                            if (payload != null)
                            {
                                if (payload.ContainsKey("component_path"))
                                {
                                    menuItem["component_path"] = payload["component_path"];
                                }
                                if (payload.ContainsKey("csharp_class"))
                                {
                                    menuItem["csharp_class"] = payload["csharp_class"];
                                }
                            }
                        }
                        catch
                        {
                            // 忽略 JSON 解析错误
                        }
                    }
                    
                    menuList.Add(menuItem);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetMenuList] 从数据库读取菜单失败: {ex.Message}");
                WriteLog($"[GetMenuList] 从数据库读取菜单失败: {ex.Message}");
                throw; // 抛出异常，不再使用降级方案
            }
            
            return menuList;
        }
        public Dictionary<string, object>? GetMenuByKey(string key)
        {
            foreach (var menu in GetMenuList())
            {
                if (menu.TryGetValue("menu_key", out var k) && k?.ToString() == key)
                    return menu;
            }
            return null;
        }

        public List<string> GetRolePermissions(int roleId)
        {
            // TODO: 从 role_permissions + permissions 表读取
            return new List<string>();
        }

        public Dictionary<string, string> GetMenuKeyMap()
        {
            var map = new Dictionary<string, string>();
            foreach (var menu in GetMenuList())
            {
                var key = menu.TryGetValue("menu_key", out var k) ? k?.ToString() : null;
                if (!string.IsNullOrEmpty(key))
                    map[key] = key;
            }
            return map;
        }

        public string? GetSystemConfig(string key)
        {
            // TODO: 读取系统配置
            return null;
        }

        /// <summary>
        /// 按配置类型读取多条系统配置（键值对列表）
        /// </summary>
        public List<Dictionary<string, object>> GetSystemConfigList(string configType)
        {
            // TODO: 从数据库 system_config 读取 config_type = configType 的记录
            return new List<Dictionary<string, object>>();
        }

        /// <summary>
        /// 保存或更新系统配置项
        /// </summary>
        public void SetSystemConfig(string configType, string key, string value)
        {
            // TODO: 写入/更新数据库 system_config 表
        }
    }
}
