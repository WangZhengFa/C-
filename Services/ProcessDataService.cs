using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 工序生产数据服务
    /// </summary>
    public class ProcessDataService
    {
        public List<ProcessDataRecord> ListAll()
        {
            const string sql = @"SELECT id, process_data_id, process_step_id, batch_no, production_date, end_date, remark
FROM process_data_main
ORDER BY id DESC";

            var result = new List<ProcessDataRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new ProcessDataRecord
                {
                    Id = GetLong(reader, "id"),
                    ProcessDataId = GetString(reader, "process_data_id"),
                    ProcessStepId = GetString(reader, "process_step_id"),
                    BatchNo = GetString(reader, "batch_no"),
                    ProductionDate = GetDate(reader, "production_date"),
                    EndDate = GetDate(reader, "end_date"),
                    Remark = GetString(reader, "remark")
                });
            }

            return result;
        }

        public long Insert(ProcessDataRecord record)
        {
            const string sql = @"INSERT INTO process_data_main
(process_data_id, process_step_id, batch_no, production_date, end_date, remark)
VALUES
(@process_data_id, @process_step_id, @batch_no, @production_date, @end_date, @remark);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(ProcessDataRecord record)
        {
            const string sql = @"UPDATE process_data_main SET
process_data_id=@process_data_id,
process_step_id=@process_step_id,
batch_no=@batch_no,
production_date=@production_date,
end_date=@end_date,
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
            const string sql = "DELETE FROM process_data_main WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, ProcessDataRecord record)
        {
            cmd.Parameters.AddWithValue("@process_data_id", (record.ProcessDataId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@process_step_id", (record.ProcessStepId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@batch_no", (record.BatchNo ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@production_date", ToDbDate(record.ProductionDate));
            cmd.Parameters.AddWithValue("@end_date", ToDbDate(record.EndDate));
            cmd.Parameters.AddWithValue("@remark", (record.Remark ?? string.Empty).Trim());
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
