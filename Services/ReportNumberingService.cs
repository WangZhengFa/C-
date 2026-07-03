using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 报告编号服务
    /// </summary>
    public class ReportNumberingService
    {
        public List<ReportNumberingRecord> ListAll()
        {
            const string sql = @"SELECT id, report_code, production_date, inspection_date, report_date,
material_id, sample_batch, sample_quantity, batch_quantity, sample_source, report_result, remark, node_code
FROM report_numbers
ORDER BY id DESC";

            var result = new List<ReportNumberingRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new ReportNumberingRecord
                {
                    Id = GetLong(reader, "id"),
                    ReportCode = GetString(reader, "report_code"),
                    ProductionDate = GetDate(reader, "production_date"),
                    InspectionDate = GetDate(reader, "inspection_date"),
                    ReportDate = GetDate(reader, "report_date"),
                    MaterialId = GetString(reader, "material_id"),
                    SampleBatch = GetString(reader, "sample_batch"),
                    SampleQuantity = GetString(reader, "sample_quantity"),
                    BatchQuantity = GetString(reader, "batch_quantity"),
                    SampleSource = GetString(reader, "sample_source"),
                    ReportResult = GetString(reader, "report_result"),
                    Remark = GetString(reader, "remark"),
                    NodeCode = GetString(reader, "node_code")
                });
            }

            return result;
        }

        public long Insert(ReportNumberingRecord record)
        {
            const string sql = @"INSERT INTO report_numbers
(report_code, production_date, inspection_date, report_date, material_id, sample_batch,
sample_quantity, batch_quantity, sample_source, report_result, remark, node_code)
VALUES
(@report_code, @production_date, @inspection_date, @report_date, @material_id, @sample_batch,
@sample_quantity, @batch_quantity, @sample_source, @report_result, @remark, @node_code);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(ReportNumberingRecord record)
        {
            const string sql = @"UPDATE report_numbers SET
report_code=@report_code,
production_date=@production_date,
inspection_date=@inspection_date,
report_date=@report_date,
material_id=@material_id,
sample_batch=@sample_batch,
sample_quantity=@sample_quantity,
batch_quantity=@batch_quantity,
sample_source=@sample_source,
report_result=@report_result,
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
            const string sql = "DELETE FROM report_numbers WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, ReportNumberingRecord record)
        {
            cmd.Parameters.AddWithValue("@report_code", (record.ReportCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@production_date", ToDbDate(record.ProductionDate));
            cmd.Parameters.AddWithValue("@inspection_date", ToDbDate(record.InspectionDate));
            cmd.Parameters.AddWithValue("@report_date", ToDbDate(record.ReportDate));
            cmd.Parameters.AddWithValue("@material_id", (record.MaterialId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sample_batch", (record.SampleBatch ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sample_quantity", (record.SampleQuantity ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@batch_quantity", (record.BatchQuantity ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sample_source", (record.SampleSource ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@report_result", (record.ReportResult ?? string.Empty).Trim());
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
