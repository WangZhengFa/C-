using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 自检数据服务
    /// </summary>
    public class SelfCheckDataService
    {
        public List<SelfCheckDataRecord> ListAll()
        {
            const string sql = @"SELECT id, self_check_id, sampling_date, report_date, inspection_id, sample_batch,
brand_series, sample_quantity, representative_quantity, sample_source, remark, node_code
FROM self_check_records
ORDER BY id DESC";

            var result = new List<SelfCheckDataRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new SelfCheckDataRecord
                {
                    Id = GetLong(reader, "id"),
                    SelfCheckId = GetString(reader, "self_check_id"),
                    SamplingDate = GetDate(reader, "sampling_date"),
                    ReportDate = GetDate(reader, "report_date"),
                    InspectionId = GetString(reader, "inspection_id"),
                    SampleBatch = GetString(reader, "sample_batch"),
                    BrandSeries = GetString(reader, "brand_series"),
                    SampleQuantity = GetString(reader, "sample_quantity"),
                    RepresentativeQuantity = GetString(reader, "representative_quantity"),
                    SampleSource = GetString(reader, "sample_source"),
                    Remark = GetString(reader, "remark"),
                    NodeCode = GetString(reader, "node_code")
                });
            }

            return result;
        }

        public long Insert(SelfCheckDataRecord record)
        {
            const string sql = @"INSERT INTO self_check_records
(self_check_id, sampling_date, report_date, inspection_id, sample_batch, brand_series,
sample_quantity, representative_quantity, sample_source, remark, node_code)
VALUES
(@self_check_id, @sampling_date, @report_date, @inspection_id, @sample_batch, @brand_series,
@sample_quantity, @representative_quantity, @sample_source, @remark, @node_code);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(SelfCheckDataRecord record)
        {
            const string sql = @"UPDATE self_check_records SET
self_check_id=@self_check_id,
sampling_date=@sampling_date,
report_date=@report_date,
inspection_id=@inspection_id,
sample_batch=@sample_batch,
brand_series=@brand_series,
sample_quantity=@sample_quantity,
representative_quantity=@representative_quantity,
sample_source=@sample_source,
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
            const string sql = "DELETE FROM self_check_records WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, SelfCheckDataRecord record)
        {
            cmd.Parameters.AddWithValue("@self_check_id", (record.SelfCheckId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sampling_date", ToDbDate(record.SamplingDate));
            cmd.Parameters.AddWithValue("@report_date", ToDbDate(record.ReportDate));
            cmd.Parameters.AddWithValue("@inspection_id", (record.InspectionId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sample_batch", (record.SampleBatch ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@brand_series", (record.BrandSeries ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sample_quantity", (record.SampleQuantity ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@representative_quantity", (record.RepresentativeQuantity ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sample_source", (record.SampleSource ?? string.Empty).Trim());
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
    }
}
