using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 报告数据服务
    /// </summary>
    public class ReportDataService
    {
        public List<ReportDataRecord> ListAll()
        {
            const string sql = @"SELECT id, node_code, report_number, sample_name, sample_batch, type,
frequency, testing_institution, testing_date, report_date, conclusion, remark
FROM report_data_main
ORDER BY id DESC";

            var result = new List<ReportDataRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new ReportDataRecord
                {
                    Id = GetLong(reader, "id"),
                    NodeCode = GetString(reader, "node_code"),
                    ReportNumber = GetString(reader, "report_number"),
                    SampleName = GetString(reader, "sample_name"),
                    SampleBatch = GetString(reader, "sample_batch"),
                    Type = GetString(reader, "type"),
                    Frequency = GetString(reader, "frequency"),
                    TestingInstitution = GetString(reader, "testing_institution"),
                    TestingDate = GetDate(reader, "testing_date"),
                    ReportDate = GetDate(reader, "report_date"),
                    Conclusion = GetString(reader, "conclusion"),
                    Remark = GetString(reader, "remark")
                });
            }

            return result;
        }

        public long Insert(ReportDataRecord record)
        {
            const string sql = @"INSERT INTO report_data_main
(node_code, report_number, sample_name, sample_batch, type, frequency, testing_institution,
 testing_date, report_date, conclusion, remark)
VALUES
(@node_code, @report_number, @sample_name, @sample_batch, @type, @frequency, @testing_institution,
 @testing_date, @report_date, @conclusion, @remark);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(ReportDataRecord record)
        {
            const string sql = @"UPDATE report_data_main SET
node_code=@node_code,
report_number=@report_number,
sample_name=@sample_name,
sample_batch=@sample_batch,
type=@type,
frequency=@frequency,
testing_institution=@testing_institution,
testing_date=@testing_date,
report_date=@report_date,
conclusion=@conclusion,
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
            const string sql = "DELETE FROM report_data_main WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, ReportDataRecord record)
        {
            cmd.Parameters.AddWithValue("@node_code", (record.NodeCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@report_number", (record.ReportNumber ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sample_name", (record.SampleName ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sample_batch", (record.SampleBatch ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@type", (record.Type ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@frequency", (record.Frequency ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@testing_institution", (record.TestingInstitution ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@testing_date", ToDbDate(record.TestingDate));
            cmd.Parameters.AddWithValue("@report_date", ToDbDate(record.ReportDate));
            cmd.Parameters.AddWithValue("@conclusion", (record.Conclusion ?? string.Empty).Trim());
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
