using System.Security.Cryptography;
using System.Text;
using FoodEnterpriseIMS.Database;
using MySqlConnector;

namespace 食品信息管理系统.Services
{
    /// <summary>
    /// 登录认证服务
    /// </summary>
    public static class AuthService
    {
        /// <summary>
        /// 验证用户名和密码
        /// </summary>
        public static async Task<(bool success, string message, string? nickname, long roleId, long userId)> ValidateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                WriteLog($"登录失败: 账号或密码为空");
                return (false, "账号或密码不能为空", null, 0, 0);
            }

            WriteLog($"尝试登录: 用户名={username}");
            var config = DbConfigService.LoadConfig();
            var connString = MysqlDbInitializer.GetConnString(config);
            WriteLog($"数据库连接参数: host={config.Host}, port={config.Port}, database={config.Database}, user={config.User}");

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();
                WriteLog("数据库连接成功");

                const string sql = @"
                    SELECT id, password_hash, is_disabled, nickname, role_id, is_online
                    FROM user_accounts
                    WHERE username = @username
                    LIMIT 1";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@username", username);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    WriteLog($"登录失败: 用户不存在 - {username}");
                    await reader.CloseAsync();
                    await RecordLoginAuditAsync(conn, null, username, false, "user_not_found");
                    return (false, "账号不存在", null, 0, 0);
                }

                if (reader.GetInt32("is_disabled") != 0)
                {
                    WriteLog($"登录失败: 账号已被禁用 - {username}");
                    var disabledUserId = reader.GetInt64("id");
                    await reader.CloseAsync();
                    await RecordLoginAuditAsync(conn, disabledUserId, username, false, "user_disabled");
                    return (false, "账号已被禁用", null, 0, 0);
                }

                var storedHash = reader.GetString("password_hash");
                var inputHash = ComputeSha256(password);
                WriteLog($"密码哈希比对: 存储={storedHash.Substring(0, Math.Min(8, storedHash.Length))}..., 输入={inputHash.Substring(0, Math.Min(8, inputHash.Length))}...");

                if (!string.Equals(storedHash, inputHash, StringComparison.OrdinalIgnoreCase))
                {
                    WriteLog($"登录失败: 密码错误 - {username}");
                    var wrongPwdUserId = reader.GetInt64("id");
                    await reader.CloseAsync();
                    await RecordLoginAuditAsync(conn, wrongPwdUserId, username, false, "wrong_password");
                    return (false, "密码错误", null, 0, 0);
                }

                var userId = reader.GetInt64("id");
                var nickname = reader.IsDBNull(reader.GetOrdinal("nickname")) ? null : reader.GetString("nickname");
                var roleId = reader.IsDBNull(reader.GetOrdinal("role_id")) ? 0 : reader.GetInt64("role_id");
                var wasOnline = !reader.IsDBNull(reader.GetOrdinal("is_online")) && reader.GetInt32("is_online") != 0;

                await reader.CloseAsync();

                await MarkLoginSuccessAsync(conn, userId, username);
                await RecordLoginAuditAsync(conn, userId, username, true, wasOnline ? "takeover_session" : "success");

                var successMessage = wasOnline ? "登录成功（已挤占原在线会话）" : "登录成功";
                WriteLog($"{successMessage}: {username} (角色ID: {roleId})");
                return (true, successMessage, nickname, roleId, userId);
            }
            catch (Exception ex)
            {
                WriteLog($"数据库连接异常: {ex.Message}\n{ex.StackTrace}");
                return (false, $"数据库连接失败：{ex.Message}", null, 0, 0);
            }
        }

        public static async Task MarkLogoutAsync(long userId)
        {
            if (userId <= 0)
            {
                return;
            }

            try
            {
                var config = DbConfigService.LoadConfig();
                var connString = MysqlDbInitializer.GetConnString(config);
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                const string sql = @"
                    UPDATE user_accounts
                    SET is_online = 0
                    WHERE id = @id";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", userId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                WriteLog($"[MarkLogoutAsync] 更新离线状态失败: {ex.Message}");
            }
        }

        private static async Task MarkLoginSuccessAsync(MySqlConnection conn, long userId, string username)
        {
            await EnsureMonthlyCounterAsync(conn, userId);

            const string updateSql = @"
                UPDATE user_accounts
                SET last_login = NOW(),
                    is_online = 1,
                    last_computer = @computer,
                    login_count_month = login_count_month + 1,
                    login_count_total = login_count_total + 1
                WHERE id = @id";

            using (var updateCmd = new MySqlCommand(updateSql, conn))
            {
                updateCmd.Parameters.AddWithValue("@id", userId);
                updateCmd.Parameters.AddWithValue("@computer", Environment.MachineName);
                await updateCmd.ExecuteNonQueryAsync();
            }

            const string statsSql = @"
                INSERT INTO login_stats(username, login_time, ip_address, user_agent)
                VALUES(@username, NOW(), '', '')";

            using var statsCmd = new MySqlCommand(statsSql, conn);
            statsCmd.Parameters.AddWithValue("@username", username);
            await statsCmd.ExecuteNonQueryAsync();
        }

        private static async Task EnsureMonthlyCounterAsync(MySqlConnection conn, long userId)
        {
            var currentMonth = DateTime.Now.ToString("yyyy-MM");
            var markerKey = $"login_month_marker:{userId}";

            string marker = string.Empty;
            const string getMarkerSql = "SELECT `value` FROM system_config WHERE `key`=@key LIMIT 1";
            using (var getMarkerCmd = new MySqlCommand(getMarkerSql, conn))
            {
                getMarkerCmd.Parameters.AddWithValue("@key", markerKey);
                var markerObj = await getMarkerCmd.ExecuteScalarAsync();
                marker = markerObj?.ToString() ?? string.Empty;
            }

            if (string.Equals(marker, currentMonth, StringComparison.Ordinal))
            {
                return;
            }

            const string resetSql = @"
                UPDATE user_accounts
                SET login_count_month = 0
                WHERE id = @id";
            using (var resetCmd = new MySqlCommand(resetSql, conn))
            {
                resetCmd.Parameters.AddWithValue("@id", userId);
                await resetCmd.ExecuteNonQueryAsync();
            }

            const string upsertMarkerSql = @"
                INSERT INTO system_config(`key`,`value`) VALUES(@key,@value)
                ON DUPLICATE KEY UPDATE `value`=VALUES(`value`)";
            using var upsertCmd = new MySqlCommand(upsertMarkerSql, conn);
            upsertCmd.Parameters.AddWithValue("@key", markerKey);
            upsertCmd.Parameters.AddWithValue("@value", currentMonth);
            await upsertCmd.ExecuteNonQueryAsync();
        }

        private static async Task RecordLoginAuditAsync(MySqlConnection conn, long? userId, string username, bool isSuccess, string reason)
        {
            const string sql = @"
                INSERT INTO login_audit_logs(user_id, username, is_success, reason, client_machine, login_time)
                VALUES(@user_id, @username, @is_success, @reason, @client_machine, NOW())";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@user_id", userId.HasValue && userId.Value > 0 ? userId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@username", username ?? string.Empty);
            cmd.Parameters.AddWithValue("@is_success", isSuccess ? 1 : 0);
            cmd.Parameters.AddWithValue("@reason", reason ?? string.Empty);
            cmd.Parameters.AddWithValue("@client_machine", Environment.MachineName);
            await cmd.ExecuteNonQueryAsync();
        }

        private static void WriteLog(string message)
        {
            try
            {
                var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [AuthService] {message}{Environment.NewLine}";
                System.IO.File.AppendAllText(logPath, logEntry);
                Console.WriteLine(logEntry.Trim());
            }
            catch
            {
                // 忽略日志写入错误
            }
        }

        /// <summary>
        /// 计算 SHA-256 哈希，与 Python hashlib.sha256().hexdigest() 结果一致
        /// </summary>
        public static string ComputeSha256(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
