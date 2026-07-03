using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 留样管理服务
    /// </summary>
    public class RetentionManagementService
    {
        public List<RetentionManagementRecord> ListAll()
        {
            const string sql = @"SELECT id, retention_code, report_code, material_id, batch_number,
retention_date, retention_person, retention_deadline, retention_location, retention_quantity,
storage_condition, sample_status, dispose_date, dispose_person, remark
FROM sample_retention_records
ORDER BY id DESC";

            var result = new List<RetentionManagementRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new RetentionManagementRecord
                {
                    Id = GetLong(reader, "id"),
                    RetentionCode = GetString(reader, "retention_code"),
                    ReportCode = GetString(reader, "report_code"),
                    MaterialId = GetString(reader, "material_id"),
                    BatchNumber = GetString(reader, "batch_number"),
                    RetentionDate = GetDate(reader, "retention_date"),
                    RetentionPerson = GetString(reader, "retention_person"),
                    RetentionDeadline = GetDate(reader, "retention_deadline"),
                    RetentionLocation = GetString(reader, "retention_location"),
                    RetentionQuantity = GetInt(reader, "retention_quantity"),
                    StorageCondition = GetString(reader, "storage_condition"),
                    SampleStatus = GetString(reader, "sample_status"),
                    DisposeDate = GetDate(reader, "dispose_date"),
                    DisposePerson = GetString(reader, "dispose_person"),
                    Remark = GetString(reader, "remark")
                });
            }

            return result;
        }

        public long Insert(RetentionManagementRecord record)
        {
            const string sql = @"INSERT INTO sample_retention_records
(retention_code, report_code, material_id, batch_number, retention_date, retention_person,
 retention_deadline, retention_location, retention_quantity, storage_condition, sample_status,
 dispose_date, dispose_person, remark)
VALUES
(@retention_code, @report_code, @material_id, @batch_number, @retention_date, @retention_person,
 @retention_deadline, @retention_location, @retention_quantity, @storage_condition, @sample_status,
 @dispose_date, @dispose_person, @remark);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(RetentionManagementRecord record)
        {
            const string sql = @"UPDATE sample_retention_records SET
retention_code=@retention_code,
report_code=@report_code,
material_id=@material_id,
batch_number=@batch_number,
retention_date=@retention_date,
retention_person=@retention_person,
retention_deadline=@retention_deadline,
retention_location=@retention_location,
retention_quantity=@retention_quantity,
storage_condition=@storage_condition,
sample_status=@sample_status,
dispose_date=@dispose_date,
dispose_person=@dispose_person,
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
            const string sql = "DELETE FROM sample_retention_records WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, RetentionManagementRecord record)
        {
            cmd.Parameters.AddWithValue("@retention_code", (record.RetentionCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@report_code", (record.ReportCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@material_id", (record.MaterialId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@batch_number", (record.BatchNumber ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@retention_date", ToDbDate(record.RetentionDate));
            cmd.Parameters.AddWithValue("@retention_person", (record.RetentionPerson ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@retention_deadline", ToDbDate(record.RetentionDeadline));
            cmd.Parameters.AddWithValue("@retention_location", (record.RetentionLocation ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@retention_quantity", record.RetentionQuantity);
            cmd.Parameters.AddWithValue("@storage_condition", (record.StorageCondition ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sample_status", string.IsNullOrWhiteSpace(record.SampleStatus) ? "在库" : record.SampleStatus.Trim());
            cmd.Parameters.AddWithValue("@dispose_date", ToDbDate(record.DisposeDate));
            cmd.Parameters.AddWithValue("@dispose_person", (record.DisposePerson ?? string.Empty).Trim());
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

        private static int GetInt(MySqlDataReader reader, string field)
        {
            var text = GetString(reader, field);
            return int.TryParse(text, out var value) ? value : 0;
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
