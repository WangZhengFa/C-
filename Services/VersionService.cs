using FoodEnterpriseIMS.Database;
using MySqlConnector;

namespace 食品信息管理系统.Services
{
    /// <summary>
    /// 版本管理服务：从数据库 system_config 表读取最新版本号，与本地版本比对
    /// </summary>
    public static class VersionService
    {
        public const string VersionConfigKey = "app_version";

        /// <summary>
        /// 从数据库读取最新版本号
        /// </summary>
        public static async Task<string?> GetLatestVersionAsync()
        {
            try
            {
                var config = DbConfigService.LoadConfig();
                var connString = MysqlDbInitializer.GetConnString(config);

                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                const string sql = "SELECT value FROM system_config WHERE `key` = @key LIMIT 1";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@key", VersionConfigKey);

                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 检查是否需要更新
        /// </summary>
        public static bool NeedUpdate(string? latestVersion)
        {
            if (string.IsNullOrWhiteSpace(latestVersion))
            {
                return false;
            }

            return !string.Equals(LocalSettingsService.LocalVersion, latestVersion, StringComparison.OrdinalIgnoreCase);
        }
    }
}
