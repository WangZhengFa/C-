using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 工序参数服务
    /// </summary>
    public class ProcessParamsService
    {
        public List<ProcessParamsRecord> ListAll()
        {
            const string sql = @"SELECT id, process_step_id, product_id, process_name, is_disabled, remark
FROM process_params_main
ORDER BY id DESC";

            var result = new List<ProcessParamsRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new ProcessParamsRecord
                {
                    Id = GetLong(reader, "id"),
                    ProcessStepId = GetString(reader, "process_step_id"),
                    ProductId = GetString(reader, "product_id"),
                    ProcessName = GetString(reader, "process_name"),
                    IsDisabled = GetBool(reader, "is_disabled"),
                    Remark = GetString(reader, "remark")
                });
            }

            return result;
        }

        public long Insert(ProcessParamsRecord record)
        {
            const string sql = @"INSERT INTO process_params_main
(process_step_id, product_id, process_name, is_disabled, remark)
VALUES
(@process_step_id, @product_id, @process_name, @is_disabled, @remark);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(ProcessParamsRecord record)
        {
            const string sql = @"UPDATE process_params_main SET
process_step_id=@process_step_id,
product_id=@product_id,
process_name=@process_name,
is_disabled=@is_disabled,
remark=@remark
WHERE id=@id";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            cmd.Parameters.AddWithValue("@id", record.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(long id)
        {
            const string sql = "DELETE FROM process_params_main WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, ProcessParamsRecord record)
        {
            cmd.Parameters.AddWithValue("@process_step_id", (record.ProcessStepId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@product_id", (record.ProductId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@process_name", (record.ProcessName ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@is_disabled", record.IsDisabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@remark", (record.Remark ?? string.Empty).Trim());
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

        private static bool GetBool(MySqlDataReader reader, string field)
        {
            var text = GetString(reader, field);
            return text == "1" || bool.TryParse(text, out var value) && value;
        }
    }
}
