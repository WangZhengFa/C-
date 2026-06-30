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
        public static async Task<(bool success, string message, string? nickname, long roleId)> ValidateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                WriteLog($"登录失败: 账号或密码为空");
                return (false, "账号或密码不能为空", null, 0);
            }

            WriteLog($"尝试登录: 用户名={username}");
            var config = DbConfigService.LoadConfig();
            var connString = MysqlDbInitializer.GetConnString(config);
            WriteLog($"数据库连接字符串: {connString}");

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();
                WriteLog("数据库连接成功");

                const string sql = @"
                    SELECT password_hash, is_disabled, nickname, role_id
                    FROM user_accounts
                    WHERE username = @username
                    LIMIT 1";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@username", username);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    WriteLog($"登录失败: 用户不存在 - {username}");
                    return (false, "账号不存在", null, 0);
                }

                if (reader.GetInt32("is_disabled") != 0)
                {
                    WriteLog($"登录失败: 账号已被禁用 - {username}");
                    return (false, "账号已被禁用", null, 0);
                }

                var storedHash = reader.GetString("password_hash");
                var inputHash = ComputeSha256(password);
                WriteLog($"密码哈希比对: 存储={storedHash.Substring(0, Math.Min(8, storedHash.Length))}..., 输入={inputHash.Substring(0, Math.Min(8, inputHash.Length))}...");

                if (!string.Equals(storedHash, inputHash, StringComparison.OrdinalIgnoreCase))
                {
                    WriteLog($"登录失败: 密码错误 - {username}");
                    return (false, "密码错误", null, 0);
                }

                var nickname = reader.IsDBNull(reader.GetOrdinal("nickname")) ? null : reader.GetString("nickname");
                var roleId = reader.GetInt64("role_id");

                WriteLog($"登录成功: {username} (角色ID: {roleId})");
                return (true, "登录成功", nickname, roleId);
            }
            catch (Exception ex)
            {
                WriteLog($"数据库连接异常: {ex.Message}\n{ex.StackTrace}");
                return (false, $"数据库连接失败：{ex.Message}", null, 0);
            }
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
