using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 外部抽检服务
    /// </summary>
    public class ExternalSamplingService
    {
        public List<ExternalSamplingRecord> ListAll()
        {
            const string sql = @"SELECT id, sampling_id, sampling_date, product_id, batch_no, product_quantity,
sampling_quantity, sampling_price, monitor_type, sampling_org, remark
FROM external_sampling
ORDER BY id DESC";

            var result = new List<ExternalSamplingRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new ExternalSamplingRecord
                {
                    Id = GetLong(reader, "id"),
                    SamplingId = GetString(reader, "sampling_id"),
                    SamplingDate = GetDate(reader, "sampling_date"),
                    ProductId = GetString(reader, "product_id"),
                    BatchNo = GetString(reader, "batch_no"),
                    ProductQuantity = GetString(reader, "product_quantity"),
                    SamplingQuantity = GetString(reader, "sampling_quantity"),
                    SamplingPrice = GetString(reader, "sampling_price"),
                    MonitorType = GetString(reader, "monitor_type"),
                    SamplingOrg = GetString(reader, "sampling_org"),
                    Remark = GetString(reader, "remark")
                });
            }

            return result;
        }

        public long Insert(ExternalSamplingRecord record)
        {
            const string sql = @"INSERT INTO external_sampling
(sampling_id, sampling_date, product_id, batch_no, product_quantity, sampling_quantity,
 sampling_price, monitor_type, sampling_org, remark)
VALUES
(@sampling_id, @sampling_date, @product_id, @batch_no, @product_quantity, @sampling_quantity,
 @sampling_price, @monitor_type, @sampling_org, @remark);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(ExternalSamplingRecord record)
        {
            const string sql = @"UPDATE external_sampling SET
sampling_id=@sampling_id,
sampling_date=@sampling_date,
product_id=@product_id,
batch_no=@batch_no,
product_quantity=@product_quantity,
sampling_quantity=@sampling_quantity,
sampling_price=@sampling_price,
monitor_type=@monitor_type,
sampling_org=@sampling_org,
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
            const string sql = "DELETE FROM external_sampling WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, ExternalSamplingRecord record)
        {
            cmd.Parameters.AddWithValue("@sampling_id", (record.SamplingId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sampling_date", ToDbDate(record.SamplingDate));
            cmd.Parameters.AddWithValue("@product_id", (record.ProductId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@batch_no", (record.BatchNo ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@product_quantity", (record.ProductQuantity ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sampling_quantity", (record.SamplingQuantity ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sampling_price", (record.SamplingPrice ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@monitor_type", (record.MonitorType ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sampling_org", (record.SamplingOrg ?? string.Empty).Trim());
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
