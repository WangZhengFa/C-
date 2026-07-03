using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 系统配置服务
    /// </summary>
    public class SystemConfigService
    {
        public List<SystemConfigRecord> ListAll()
        {
            const string sql = @"SELECT `key`, `value`
FROM system_config
ORDER BY `key`";

            var result = new List<SystemConfigRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new SystemConfigRecord
                {
                    ConfigKey = GetString(reader, "key"),
                    Value = GetString(reader, "value")
                });
            }

            return result;
        }

        public void Upsert(SystemConfigRecord record)
        {
            const string sql = @"INSERT INTO system_config(`key`, `value`)
VALUES(@key, @value)
ON DUPLICATE KEY UPDATE `value` = VALUES(`value`)";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@key", (record.ConfigKey ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@value", record.Value ?? string.Empty);
            cmd.ExecuteNonQuery();
        }

        public void Delete(string configKey)
        {
            const string sql = "DELETE FROM system_config WHERE `key`=@key";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@key", (configKey ?? string.Empty).Trim());
            cmd.ExecuteNonQuery();
        }

        private static MySqlConnection CreateConnection()
        {
            var cfg = MysqlDbInitializer.LoadMysqlConfig();
            var conn = new MySqlConnection(MysqlDbInitializer.GetConnString(cfg));
            conn.Open();
            return conn;
        }

        private static string GetString(MySqlDataReader reader, string field)
        {
            var index = reader.GetOrdinal(field);
            return reader.IsDBNull(index) ? string.Empty : reader.GetValue(index)?.ToString() ?? string.Empty;
        }
    }
}
