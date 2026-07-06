using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 标准规范服务
    /// </summary>
    public class StandardRegulationsService
    {
        public List<StandardRegulationsRecord> ListAll()
        {
            const string sql = @"SELECT standard_id, node_code, category, series, standard_name, standard_code,
publish_dept, publish_year, applies_to_haccp, publish_date, implement_date, revision_date, effective_date,
standard_link, new_standard_link, is_invalid, remark, sort_order, is_enabled
FROM standard_specifications
ORDER BY sort_order ASC, standard_id DESC";

            var result = new List<StandardRegulationsRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new StandardRegulationsRecord
                {
                    StandardId = GetString(reader, "standard_id"),
                    NodeCode = GetString(reader, "node_code"),
                    Category = GetString(reader, "category"),
                    Series = GetString(reader, "series"),
                    StandardName = GetString(reader, "standard_name"),
                    StandardCode = GetString(reader, "standard_code"),
                    PublishDept = GetString(reader, "publish_dept"),
                    PublishYear = GetString(reader, "publish_year"),
                    AppliesToHaccp = GetBool(reader, "applies_to_haccp"),
                    PublishDate = GetDate(reader, "publish_date"),
                    ImplementDate = GetDate(reader, "implement_date"),
                    RevisionDate = GetDate(reader, "revision_date"),
                    EffectiveDate = GetDate(reader, "effective_date"),
                    StandardLink = GetString(reader, "standard_link"),
                    NewStandardLink = GetString(reader, "new_standard_link"),
                    IsInvalid = GetBool(reader, "is_invalid"),
                    Remark = GetString(reader, "remark"),
                    SortOrder = GetInt(reader, "sort_order"),
                    IsEnabled = GetBool(reader, "is_enabled")
                });
            }

            return result;
        }

        public long Insert(StandardRegulationsRecord record)
        {
            const string sql = @"INSERT INTO standard_specifications
(standard_id, node_code, category, series, standard_name, standard_code, publish_dept, publish_year,
 applies_to_haccp, publish_date, implement_date, revision_date, effective_date, standard_link,
 new_standard_link, is_invalid, remark, sort_order, is_enabled)
VALUES
(@standard_id, @node_code, @category, @series, @standard_name, @standard_code, @publish_dept, @publish_year,
 @applies_to_haccp, @publish_date, @implement_date, @revision_date, @effective_date, @standard_link,
 @new_standard_link, @is_invalid, @remark, @sort_order, @is_enabled);";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var affected = cmd.ExecuteNonQuery();
            return affected > 0 ? 1 : 0;
        }

        public void Update(StandardRegulationsRecord record, string? originalStandardId = null)
        {
            const string sql = @"UPDATE standard_specifications SET
standard_id=@standard_id,
node_code=@node_code,
category=@category,
series=@series,
standard_name=@standard_name,
standard_code=@standard_code,
publish_dept=@publish_dept,
publish_year=@publish_year,
applies_to_haccp=@applies_to_haccp,
publish_date=@publish_date,
implement_date=@implement_date,
revision_date=@revision_date,
effective_date=@effective_date,
standard_link=@standard_link,
new_standard_link=@new_standard_link,
is_invalid=@is_invalid,
remark=@remark,
sort_order=@sort_order,
is_enabled=@is_enabled
WHERE standard_id=@original_standard_id";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            cmd.Parameters.AddWithValue("@original_standard_id", string.IsNullOrWhiteSpace(originalStandardId) ? record.StandardId : originalStandardId);
            cmd.ExecuteNonQuery();
        }

        public void Delete(string standardId)
        {
            const string sql = "DELETE FROM standard_specifications WHERE standard_id=@standard_id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@standard_id", (standardId ?? string.Empty).Trim());
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, StandardRegulationsRecord record)
        {
            cmd.Parameters.AddWithValue("@standard_id", (record.StandardId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@node_code", (record.NodeCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@category", (record.Category ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@series", (record.Series ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@standard_name", (record.StandardName ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@standard_code", (record.StandardCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@publish_dept", (record.PublishDept ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@publish_year", (record.PublishYear ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@applies_to_haccp", record.AppliesToHaccp ? 1 : 0);
            cmd.Parameters.AddWithValue("@publish_date", ToDbDate(record.PublishDate));
            cmd.Parameters.AddWithValue("@implement_date", ToDbDate(record.ImplementDate));
            cmd.Parameters.AddWithValue("@revision_date", ToDbDate(record.RevisionDate));
            cmd.Parameters.AddWithValue("@effective_date", ToDbDate(record.EffectiveDate));
            cmd.Parameters.AddWithValue("@standard_link", (record.StandardLink ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@new_standard_link", (record.NewStandardLink ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@is_invalid", record.IsInvalid ? 1 : 0);
            cmd.Parameters.AddWithValue("@remark", (record.Remark ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sort_order", record.SortOrder);
            cmd.Parameters.AddWithValue("@is_enabled", record.IsEnabled ? 1 : 0);
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

        private static int GetInt(MySqlDataReader reader, string field)
        {
            var text = GetString(reader, field);
            return int.TryParse(text, out var value) ? value : 0;
        }

        private static bool GetBool(MySqlDataReader reader, string field)
        {
            var text = GetString(reader, field);
            return text == "1" || bool.TryParse(text, out var value) && value;
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
