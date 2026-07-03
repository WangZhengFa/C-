using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 通用字典字段定义服务
    /// </summary>
    public class CommonDictFieldDefsService
    {
        public List<CommonDictFieldDefsRecord> ListAll()
        {
            const string sql = @"SELECT id, field_key, field_label, field_type, placeholder, min_value, max_value,
options, description, sort_order, is_enabled, node_code
FROM common_dict_field_defs
ORDER BY sort_order ASC, id DESC";

            var result = new List<CommonDictFieldDefsRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new CommonDictFieldDefsRecord
                {
                    Id = GetLong(reader, "id"),
                    FieldKey = GetString(reader, "field_key"),
                    FieldLabel = GetString(reader, "field_label"),
                    FieldType = GetString(reader, "field_type"),
                    Placeholder = GetString(reader, "placeholder"),
                    MinValue = GetInt(reader, "min_value"),
                    MaxValue = GetInt(reader, "max_value"),
                    Options = GetString(reader, "options"),
                    Description = GetString(reader, "description"),
                    SortOrder = GetInt(reader, "sort_order"),
                    IsEnabled = GetBool(reader, "is_enabled"),
                    NodeCode = GetString(reader, "node_code")
                });
            }

            return result;
        }

        public long Insert(CommonDictFieldDefsRecord record)
        {
            const string sql = @"INSERT INTO common_dict_field_defs
(field_key, field_label, field_type, placeholder, min_value, max_value, options, description, sort_order, is_enabled, node_code)
VALUES
(@field_key, @field_label, @field_type, @placeholder, @min_value, @max_value, @options, @description, @sort_order, @is_enabled, @node_code);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(CommonDictFieldDefsRecord record)
        {
            const string sql = @"UPDATE common_dict_field_defs SET
field_key=@field_key,
field_label=@field_label,
field_type=@field_type,
placeholder=@placeholder,
min_value=@min_value,
max_value=@max_value,
options=@options,
description=@description,
sort_order=@sort_order,
is_enabled=@is_enabled,
node_code=@node_code
WHERE id=@id";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            cmd.Parameters.AddWithValue("@id", record.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(long id)
        {
            const string sql = "DELETE FROM common_dict_field_defs WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, CommonDictFieldDefsRecord record)
        {
            cmd.Parameters.AddWithValue("@field_key", (record.FieldKey ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@field_label", (record.FieldLabel ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@field_type", (record.FieldType ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@placeholder", (record.Placeholder ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@min_value", record.MinValue);
            cmd.Parameters.AddWithValue("@max_value", record.MaxValue);
            cmd.Parameters.AddWithValue("@options", (record.Options ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@description", (record.Description ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sort_order", record.SortOrder);
            cmd.Parameters.AddWithValue("@is_enabled", record.IsEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@node_code", (record.NodeCode ?? string.Empty).Trim());
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
    }
}
