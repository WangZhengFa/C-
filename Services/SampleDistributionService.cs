using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 样品分发服务
    /// </summary>
    public class SampleDistributionService
    {
        public List<SampleDistributionRecord> ListAll()
        {
            const string sql = @"SELECT id, receive_send_id, receive_send_date, inspection_date, report_date,
sample_name, sample_batch, sample_quantity, retention_quantity, representative_quantity,
sample_source, is_reinspection, remark, node_code
FROM sample_receive_send
ORDER BY id DESC";

            var result = new List<SampleDistributionRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new SampleDistributionRecord
                {
                    Id = GetLong(reader, "id"),
                    ReceiveSendId = GetString(reader, "receive_send_id"),
                    ReceiveSendDate = GetDate(reader, "receive_send_date"),
                    InspectionDate = GetDate(reader, "inspection_date"),
                    ReportDate = GetDate(reader, "report_date"),
                    SampleName = GetString(reader, "sample_name"),
                    SampleBatch = GetString(reader, "sample_batch"),
                    SampleQuantity = GetString(reader, "sample_quantity"),
                    RetentionQuantity = GetString(reader, "retention_quantity"),
                    RepresentativeQuantity = GetString(reader, "representative_quantity"),
                    SampleSource = GetString(reader, "sample_source"),
                    IsReinspection = GetBool(reader, "is_reinspection"),
                    Remark = GetString(reader, "remark"),
                    NodeCode = GetString(reader, "node_code")
                });
            }

            return result;
        }

        public long Insert(SampleDistributionRecord record)
        {
            const string sql = @"INSERT INTO sample_receive_send
(receive_send_id, receive_send_date, inspection_date, report_date, sample_name, sample_batch,
sample_quantity, retention_quantity, representative_quantity, sample_source, is_reinspection, remark, node_code)
VALUES
(@receive_send_id, @receive_send_date, @inspection_date, @report_date, @sample_name, @sample_batch,
@sample_quantity, @retention_quantity, @representative_quantity, @sample_source, @is_reinspection, @remark, @node_code);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(SampleDistributionRecord record)
        {
            const string sql = @"UPDATE sample_receive_send SET
receive_send_id=@receive_send_id,
receive_send_date=@receive_send_date,
inspection_date=@inspection_date,
report_date=@report_date,
sample_name=@sample_name,
sample_batch=@sample_batch,
sample_quantity=@sample_quantity,
retention_quantity=@retention_quantity,
representative_quantity=@representative_quantity,
sample_source=@sample_source,
is_reinspection=@is_reinspection,
remark=@remark,
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
            const string sql = "DELETE FROM sample_receive_send WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, SampleDistributionRecord record)
        {
            cmd.Parameters.AddWithValue("@receive_send_id", (record.ReceiveSendId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@receive_send_date", ToDbDate(record.ReceiveSendDate));
            cmd.Parameters.AddWithValue("@inspection_date", ToDbDate(record.InspectionDate));
            cmd.Parameters.AddWithValue("@report_date", ToDbDate(record.ReportDate));
            cmd.Parameters.AddWithValue("@sample_name", (record.SampleName ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sample_batch", (record.SampleBatch ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sample_quantity", (record.SampleQuantity ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@retention_quantity", (record.RetentionQuantity ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@representative_quantity", (record.RepresentativeQuantity ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sample_source", (record.SampleSource ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@is_reinspection", record.IsReinspection ? 1 : 0);
            cmd.Parameters.AddWithValue("@remark", (record.Remark ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@node_code", (record.NodeCode ?? string.Empty).Trim());
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

        private static bool GetBool(MySqlDataReader reader, string field)
        {
            var text = GetString(reader, field);
            return text == "1" || bool.TryParse(text, out var v) && v;
        }
    }
}
