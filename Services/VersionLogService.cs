using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 版本更新日志服务
    /// </summary>
    public class VersionLogService
    {
        public List<VersionLogRecord> ListAll()
        {
            const string sql = @"SELECT id, version, update_date, description
FROM version_log
ORDER BY update_date DESC, id DESC";

            var result = new List<VersionLogRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new VersionLogRecord
                {
                    Id = GetLong(reader, "id"),
                    Version = GetString(reader, "version"),
                    UpdateDate = GetDate(reader, "update_date"),
                    Description = GetString(reader, "description")
                });
            }

            return result;
        }

        public long Insert(VersionLogRecord record)
        {
            const string sql = @"INSERT INTO version_log
(version, update_date, description)
VALUES
(@version, @update_date, @description);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(VersionLogRecord record)
        {
            const string sql = @"UPDATE version_log SET
version=@version,
update_date=@update_date,
description=@description
WHERE id=@id";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            cmd.Parameters.AddWithValue("@id", record.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(long id)
        {
            const string sql = "DELETE FROM version_log WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, VersionLogRecord record)
        {
            cmd.Parameters.AddWithValue("@version", (record.Version ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@update_date", ToDbDate(record.UpdateDate));
            cmd.Parameters.AddWithValue("@description", (record.Description ?? string.Empty).Trim());
        }

        private static object ToDbDate(DateTime? date)
        {
            return date.HasValue ? date.Value.Date : DBNull.Value;
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

        private static long GetLong(MySqlDataReader reader, string field)
        {
            var text = GetString(reader, field);
            return long.TryParse(text, out var value) ? value : 0;
        }

        private static DateTime? GetDate(MySqlDataReader reader, string field)
        {
            var text = GetString(reader, field);
            if (string.IsNullOrWhiteSpace(text) || text.StartsWith("0000-00-00"))
            {
                return null;
            }

            return DateTime.TryParse(text, out var value) ? value.Date : null;
        }
    }
}
