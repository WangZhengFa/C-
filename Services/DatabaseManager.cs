using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

        private static string BuildConnString()
        {
            var cfg = FoodEnterpriseIMS.Database.MysqlDbInitializer.LoadMysqlConfig();
            return $"server={cfg.Host};port={cfg.Port};user={cfg.User};password={cfg.Password};database={cfg.Database};charset=utf8mb4;Pooling=true;Max Pool Size=10;Min Pool Size=1";
        }

        private static MySqlConnection OpenConnection()
        {
            var conn = new MySqlConnection(BuildConnString());
            conn.Open();
            return conn;
        }

        private static string ComputeSha256(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
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
            if (!CheckConnection())
            {
                throw new InvalidOperationException("数据库重连失败，请检查 MySQL 配置或网络连接。");
            }
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
                    
                    // 解析 payload_json 获取 component_path、csharp_component_path、csharp_class 等信息
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
                                if (payload.ContainsKey("csharp_component_path"))
                                {
                                    menuItem["csharp_component_path"] = payload["csharp_component_path"];
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
            var permissions = new List<string>();
            try
            {
                var cfg = FoodEnterpriseIMS.Database.MysqlDbInitializer.LoadMysqlConfig();
                var connStr = $"server={cfg.Host};port={cfg.Port};user={cfg.User};password={cfg.Password};database={cfg.Database};charset=utf8mb4;Pooling=true;Max Pool Size=10;Min Pool Size=1";

                using var conn = new MySqlConnection(connStr);
                conn.Open();

                const string sql = @"
                    SELECT p.permission_key
                    FROM role_permissions rp
                    INNER JOIN permissions p ON p.id = rp.permission_id
                    WHERE rp.role_id = @roleId";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@roleId", roleId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        permissions.Add(reader.GetString(0));
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"[GetRolePermissions] 查询角色权限失败: {ex.Message}");
            }

            return permissions;
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
            try
            {
                var cfg = FoodEnterpriseIMS.Database.MysqlDbInitializer.LoadMysqlConfig();
                var connStr = $"server={cfg.Host};port={cfg.Port};user={cfg.User};password={cfg.Password};database={cfg.Database};charset=utf8mb4;Pooling=true;Max Pool Size=10;Min Pool Size=1";

                using var conn = new MySqlConnection(connStr);
                conn.Open();

                const string sql = "SELECT `value` FROM system_config WHERE `key` = @key LIMIT 1";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@key", key);
                var result = cmd.ExecuteScalar();
                return result?.ToString();
            }
            catch (Exception ex)
            {
                WriteLog($"[GetSystemConfig] 读取系统配置失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 按配置类型读取多条系统配置（键值对列表）
        /// </summary>
        public List<Dictionary<string, object>> GetSystemConfigList(string configType)
        {
            var list = new List<Dictionary<string, object>>();
            try
            {
                // 当前库表结构为 key/value，采用 "configType:key" 约定进行分组。
                var prefix = $"{configType}:";

                var cfg = FoodEnterpriseIMS.Database.MysqlDbInitializer.LoadMysqlConfig();
                var connStr = $"server={cfg.Host};port={cfg.Port};user={cfg.User};password={cfg.Password};database={cfg.Database};charset=utf8mb4;Pooling=true;Max Pool Size=10;Min Pool Size=1";

                using var conn = new MySqlConnection(connStr);
                conn.Open();

                const string sql = @"
                    SELECT `key`, `value`
                    FROM system_config
                    WHERE `key` LIKE @prefix
                    ORDER BY `key` ASC";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@prefix", prefix + "%");

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var fullKey = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                    var configKey = fullKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        ? fullKey[prefix.Length..]
                        : fullKey;
                    var configValue = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

                    list.Add(new Dictionary<string, object>
                    {
                        ["config_type"] = configType,
                        ["config_key"] = configKey,
                        ["config_value"] = configValue
                    });
                }
            }
            catch (Exception ex)
            {
                WriteLog($"[GetSystemConfigList] 读取系统配置列表失败: {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// 保存或更新系统配置项
        /// </summary>
        public void SetSystemConfig(string configType, string key, string value)
        {
            try
            {
                var fullKey = $"{configType}:{key}";

                var cfg = FoodEnterpriseIMS.Database.MysqlDbInitializer.LoadMysqlConfig();
                var connStr = $"server={cfg.Host};port={cfg.Port};user={cfg.User};password={cfg.Password};database={cfg.Database};charset=utf8mb4;Pooling=true;Max Pool Size=10;Min Pool Size=1";

                using var conn = new MySqlConnection(connStr);
                conn.Open();

                const string sql = @"
                    INSERT INTO system_config(`key`, `value`)
                    VALUES(@key, @value)
                    ON DUPLICATE KEY UPDATE `value` = VALUES(`value`)";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@key", fullKey);
                cmd.Parameters.AddWithValue("@value", value ?? string.Empty);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                WriteLog($"[SetSystemConfig] 保存系统配置失败: {ex.Message}");
                throw;
            }
        }

        public List<Dictionary<string, object>> GetAllRoles()
        {
            var list = new List<Dictionary<string, object>>();
            try
            {
                using var conn = OpenConnection();
                const string sql = @"
                    SELECT id, name, description, is_enabled
                    FROM roles
                    ORDER BY id";
                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Dictionary<string, object>
                    {
                        ["id"] = reader.GetInt64("id"),
                        ["name"] = reader.IsDBNull(reader.GetOrdinal("name")) ? string.Empty : reader.GetString("name"),
                        ["description"] = reader.IsDBNull(reader.GetOrdinal("description")) ? string.Empty : reader.GetString("description"),
                        ["is_enabled"] = reader.IsDBNull(reader.GetOrdinal("is_enabled")) ? 1 : reader.GetInt32("is_enabled")
                    });
                }
            }
            catch (Exception ex)
            {
                WriteLog($"[GetAllRoles] 读取角色列表失败: {ex.Message}");
            }

            return list;
        }

        public long AddRole(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("角色名称不能为空", nameof(name));
            }

            using var conn = OpenConnection();
            const string sql = @"
                INSERT INTO roles(name, description, is_enabled)
                VALUES(@name, @description, 1);
                SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", name.Trim());
            cmd.Parameters.AddWithValue("@description", description?.Trim() ?? string.Empty);
            var result = cmd.ExecuteScalar();
            return Convert.ToInt64(result);
        }

        public bool UpdateRole(long roleId, string name, string description)
        {
            using var conn = OpenConnection();
            const string sql = @"
                UPDATE roles
                SET name = @name,
                    description = @description
                WHERE id = @id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", roleId);
            cmd.Parameters.AddWithValue("@name", name?.Trim() ?? string.Empty);
            cmd.Parameters.AddWithValue("@description", description?.Trim() ?? string.Empty);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool DeleteRole(long roleId)
        {
            if (roleId == 1)
            {
                throw new InvalidOperationException("系统管理员角色不可删除");
            }

            var boundUsers = CountUsersByRole(roleId);
            if (boundUsers > 0)
            {
                throw new InvalidOperationException($"该角色下仍有关联用户（{boundUsers}人），请先调整用户角色后再删除。");
            }

            using var conn = OpenConnection();
            const string sql = "DELETE FROM roles WHERE id = @id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", roleId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public int CountUsersByRole(long roleId)
        {
            try
            {
                using var conn = OpenConnection();
                const string sql = "SELECT COUNT(*) FROM user_accounts WHERE role_id = @roleId";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@roleId", roleId);
                var result = cmd.ExecuteScalar();
                return Convert.ToInt32(result ?? 0);
            }
            catch (Exception ex)
            {
                WriteLog($"[CountUsersByRole] 统计角色关联用户失败: {ex.Message}");
                return 0;
            }
        }

        public List<Dictionary<string, object>> GetAllUsers()
        {
            var list = new List<Dictionary<string, object>>();
            try
            {
                using var conn = OpenConnection();
                const string sql = @"
                    SELECT u.id, u.username, u.nickname, u.department, u.role_id,
                           IFNULL(r.name, '') AS role_name,
                           u.is_disabled, u.last_login, u.login_count_month, u.login_count_total
                    FROM user_accounts u
                    LEFT JOIN roles r ON r.id = u.role_id
                    ORDER BY u.id";
                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Dictionary<string, object>
                    {
                        ["id"] = reader.GetInt64("id"),
                        ["username"] = reader.GetString("username"),
                        ["nickname"] = reader.IsDBNull(reader.GetOrdinal("nickname")) ? string.Empty : reader.GetString("nickname"),
                        ["department"] = reader.IsDBNull(reader.GetOrdinal("department")) ? string.Empty : reader.GetString("department"),
                        ["role_id"] = reader.IsDBNull(reader.GetOrdinal("role_id")) ? 0L : reader.GetInt64("role_id"),
                        ["role_name"] = reader.IsDBNull(reader.GetOrdinal("role_name")) ? string.Empty : reader.GetString("role_name"),
                        ["is_disabled"] = reader.IsDBNull(reader.GetOrdinal("is_disabled")) ? 0 : reader.GetInt32("is_disabled"),
                        ["last_login"] = reader.IsDBNull(reader.GetOrdinal("last_login")) ? string.Empty : reader.GetDateTime("last_login").ToString("yyyy-MM-dd HH:mm:ss"),
                        ["login_count_month"] = reader.IsDBNull(reader.GetOrdinal("login_count_month")) ? 0 : reader.GetInt32("login_count_month"),
                        ["login_count_total"] = reader.IsDBNull(reader.GetOrdinal("login_count_total")) ? 0 : reader.GetInt32("login_count_total")
                    });
                }
            }
            catch (Exception ex)
            {
                WriteLog($"[GetAllUsers] 读取用户列表失败: {ex.Message}");
            }

            return list;
        }

        public long AddUser(string username, string password, long roleId, string nickname, string department)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("用户名不能为空", nameof(username));
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("密码不能为空", nameof(password));
            }

            using var conn = OpenConnection();
            const string sql = @"
                INSERT INTO user_accounts(username, password_hash, role_id, nickname, department, is_disabled)
                VALUES(@username, @password_hash, @role_id, @nickname, @department, 0);
                SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", username.Trim());
            cmd.Parameters.AddWithValue("@password_hash", ComputeSha256(password));
            cmd.Parameters.AddWithValue("@role_id", roleId <= 0 ? 2 : roleId);
            cmd.Parameters.AddWithValue("@nickname", nickname?.Trim() ?? string.Empty);
            cmd.Parameters.AddWithValue("@department", department?.Trim() ?? string.Empty);
            var result = cmd.ExecuteScalar();
            return Convert.ToInt64(result);
        }

        public bool UpdateUser(long userId, string nickname, string department, long roleId, bool isDisabled, string? newPassword = null)
        {
            using var conn = OpenConnection();
            var sql = @"
                UPDATE user_accounts
                SET nickname = @nickname,
                    department = @department,
                    role_id = @role_id,
                    is_disabled = @is_disabled";

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                sql += ", password_hash = @password_hash";
            }

            sql += " WHERE id = @id";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", userId);
            cmd.Parameters.AddWithValue("@nickname", nickname?.Trim() ?? string.Empty);
            cmd.Parameters.AddWithValue("@department", department?.Trim() ?? string.Empty);
            cmd.Parameters.AddWithValue("@role_id", roleId <= 0 ? 2 : roleId);
            cmd.Parameters.AddWithValue("@is_disabled", isDisabled ? 1 : 0);
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                cmd.Parameters.AddWithValue("@password_hash", ComputeSha256(newPassword));
            }

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool DeleteUser(long userId)
        {
            using var conn = OpenConnection();

            const string checkSql = "SELECT username, role_id FROM user_accounts WHERE id = @id LIMIT 1";
            using var checkCmd = new MySqlCommand(checkSql, conn);
            checkCmd.Parameters.AddWithValue("@id", userId);
            using var reader = checkCmd.ExecuteReader();
            if (!reader.Read())
            {
                return false;
            }

            var username = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            var roleId = reader.IsDBNull(1) ? 0L : reader.GetInt64(1);
            reader.Close();

            if (username == "admin" && roleId == 1)
            {
                throw new InvalidOperationException("系统管理员账号 admin 受保护，无法删除");
            }

            const string deleteSql = "DELETE FROM user_accounts WHERE id = @id";
            using var deleteCmd = new MySqlCommand(deleteSql, conn);
            deleteCmd.Parameters.AddWithValue("@id", userId);
            return deleteCmd.ExecuteNonQuery() > 0;
        }

        public bool ChangeUserPassword(long userId, string oldPassword, string newPassword)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("用户ID无效", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(oldPassword))
            {
                throw new ArgumentException("旧密码不能为空", nameof(oldPassword));
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                throw new ArgumentException("新密码不能为空", nameof(newPassword));
            }

            if (string.Equals(oldPassword, newPassword, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("新密码不能与旧密码相同");
            }

            using var conn = OpenConnection();

            const string getSql = "SELECT password_hash FROM user_accounts WHERE id = @id LIMIT 1";
            string? storedHash;
            using (var getCmd = new MySqlCommand(getSql, conn))
            {
                getCmd.Parameters.AddWithValue("@id", userId);
                storedHash = getCmd.ExecuteScalar()?.ToString();
            }

            if (string.IsNullOrWhiteSpace(storedHash))
            {
                throw new InvalidOperationException("未找到用户账号");
            }

            var oldHash = ComputeSha256(oldPassword);
            if (!string.Equals(storedHash, oldHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("旧密码错误");
            }

            const string updateSql = "UPDATE user_accounts SET password_hash = @password_hash WHERE id = @id";
            using var updateCmd = new MySqlCommand(updateSql, conn);
            updateCmd.Parameters.AddWithValue("@id", userId);
            updateCmd.Parameters.AddWithValue("@password_hash", ComputeSha256(newPassword));
            return updateCmd.ExecuteNonQuery() > 0;
        }

        public List<Dictionary<string, object>> GetMenuButtonPermissions()
        {
            var list = new List<Dictionary<string, object>>();
            try
            {
                using var conn = OpenConnection();
                const string sql = @"
                    SELECT id, permission_key, name, node_type, node_code, description
                    FROM permissions
                    WHERE node_type IN ('menu','button')
                    ORDER BY node_type, id";
                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Dictionary<string, object>
                    {
                        ["id"] = reader.GetInt64("id"),
                        ["permission_key"] = reader.GetString("permission_key"),
                        ["name"] = reader.IsDBNull(reader.GetOrdinal("name")) ? string.Empty : reader.GetString("name"),
                        ["node_type"] = reader.IsDBNull(reader.GetOrdinal("node_type")) ? string.Empty : reader.GetString("node_type"),
                        ["node_code"] = reader.IsDBNull(reader.GetOrdinal("node_code")) ? string.Empty : reader.GetString("node_code"),
                        ["description"] = reader.IsDBNull(reader.GetOrdinal("description")) ? string.Empty : reader.GetString("description")
                    });
                }
            }
            catch (Exception ex)
            {
                WriteLog($"[GetMenuButtonPermissions] 读取权限失败: {ex.Message}");
            }

            return list;
        }

        public List<long> GetRolePermissionIds(long roleId)
        {
            var ids = new List<long>();
            try
            {
                using var conn = OpenConnection();
                const string sql = "SELECT permission_id FROM role_permissions WHERE role_id = @roleId";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@roleId", roleId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        ids.Add(reader.GetInt64(0));
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"[GetRolePermissionIds] 读取角色权限ID失败: {ex.Message}");
            }

            return ids;
        }

        public void SaveRolePermissions(long roleId, IEnumerable<long> permissionIds)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                using (var deleteCmd = new MySqlCommand("DELETE FROM role_permissions WHERE role_id = @roleId", conn, tx))
                {
                    deleteCmd.Parameters.AddWithValue("@roleId", roleId);
                    deleteCmd.ExecuteNonQuery();
                }

                foreach (var permissionId in permissionIds.Distinct())
                {
                    using var insertCmd = new MySqlCommand(@"
                        INSERT IGNORE INTO role_permissions(role_id, permission_id)
                        VALUES(@roleId, @permissionId)", conn, tx);
                    insertCmd.Parameters.AddWithValue("@roleId", roleId);
                    insertCmd.Parameters.AddWithValue("@permissionId", permissionId);
                    insertCmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                WriteLog($"[SaveRolePermissions] 保存角色权限失败: {ex.Message}");
                throw;
            }
        }

        public void SyncMenuPermissions()
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                using (var syncMenuCmd = new MySqlCommand(@"
INSERT INTO permissions(permission_key,name,node_type,description,node_code)
SELECT tn.node_code, tn.title, 'menu', tn.title, tn.node_code
FROM tree_nodes tn
WHERE tn.tree_key='system_menu' AND COALESCE(tn.node_code,'')<>''
ON DUPLICATE KEY UPDATE
  name=VALUES(name),
  node_type='menu',
  description=VALUES(description),
  node_code=VALUES(node_code);", conn, tx))
                {
                    syncMenuCmd.ExecuteNonQuery();
                }

                using (var syncButtonCmd = new MySqlCommand(@"
INSERT INTO permissions(permission_key,name,node_type,description,node_code)
SELECT CONCAT(mb.node_code,':',mb.button_key),
       COALESCE(NULLIF(mb.button_name_cn,''), mb.button_key),
       'button',
       mb.description,
       mb.node_code
FROM menu_buttons mb
WHERE mb.enabled=1
  AND COALESCE(mb.node_code,'')<>''
  AND COALESCE(mb.button_key,'')<>''
ON DUPLICATE KEY UPDATE
  name=VALUES(name),
  node_type='button',
  description=VALUES(description),
  node_code=VALUES(node_code);", conn, tx))
                {
                    syncButtonCmd.ExecuteNonQuery();
                }

                using (var cleanupMenuCmd = new MySqlCommand(@"
DELETE p
FROM permissions p
LEFT JOIN tree_nodes tn
  ON tn.tree_key='system_menu'
 AND tn.node_code=p.permission_key
WHERE p.node_type='menu'
  AND tn.node_code IS NULL;", conn, tx))
                {
                    cleanupMenuCmd.ExecuteNonQuery();
                }

                using (var cleanupButtonCmd = new MySqlCommand(@"
DELETE p
FROM permissions p
LEFT JOIN menu_buttons mb
  ON p.node_type='button'
 AND p.node_code=mb.node_code
 AND p.permission_key=CONCAT(mb.node_code,':',mb.button_key)
 AND mb.enabled=1
WHERE p.node_type='button'
  AND mb.id IS NULL;", conn, tx))
                {
                    cleanupButtonCmd.ExecuteNonQuery();
                }

                using (var cleanupRolePermCmd = new MySqlCommand(@"
DELETE rp
FROM role_permissions rp
LEFT JOIN permissions p ON p.id=rp.permission_id
WHERE p.id IS NULL;", conn, tx))
                {
                    cleanupRolePermCmd.ExecuteNonQuery();
                }

                using (var grantAdminCmd = new MySqlCommand(@"
INSERT IGNORE INTO role_permissions(role_id, permission_id)
SELECT 1, p.id
FROM permissions p
WHERE p.node_type IN ('menu','button');", conn, tx))
                {
                    grantAdminCmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                WriteLog($"[SyncMenuPermissions] 同步权限失败: {ex.Message}");
                throw;
            }
        }

        public List<string> GetDefinedButtonPermissionKeys(string menuKey)
        {
            var keys = new List<string>();
            if (string.IsNullOrWhiteSpace(menuKey))
            {
                return keys;
            }

            try
            {
                using var conn = OpenConnection();
                const string sql = @"
                    SELECT permission_key
                    FROM permissions
                    WHERE node_type = 'button'
                      AND node_code = @menuKey
                    ORDER BY id";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@menuKey", menuKey);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        keys.Add(reader.GetString(0));
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"[GetDefinedButtonPermissionKeys] 读取按钮权限定义失败: {ex.Message}");
            }

            return keys;
        }
    }
}
