using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 型式检验服务
    /// </summary>
    public class TypeInspectionService
    {
        public List<TypeInspectionRecord> ListAll()
        {
            const string sql = @"SELECT id, inspection_id, product_id, batch_no, send_date, report_date, conclusion, testing_org, remark
FROM type_inspection_records
ORDER BY id DESC";

            var result = new List<TypeInspectionRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new TypeInspectionRecord
                {
                    Id = GetLong(reader, "id"),
                    InspectionId = GetString(reader, "inspection_id"),
                    ProductId = GetString(reader, "product_id"),
                    BatchNo = GetString(reader, "batch_no"),
                    SendDate = GetDate(reader, "send_date"),
                    ReportDate = GetDate(reader, "report_date"),
                    Conclusion = GetString(reader, "conclusion"),
                    TestingOrg = GetString(reader, "testing_org"),
                    Remark = GetString(reader, "remark")
                });
            }

            return result;
        }

        public long Insert(TypeInspectionRecord record)
        {
            const string sql = @"INSERT INTO type_inspection_records
(inspection_id, product_id, batch_no, send_date, report_date, conclusion, testing_org, remark)
VALUES
(@inspection_id, @product_id, @batch_no, @send_date, @report_date, @conclusion, @testing_org, @remark);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(TypeInspectionRecord record)
        {
            const string sql = @"UPDATE type_inspection_records SET
inspection_id=@inspection_id,
product_id=@product_id,
batch_no=@batch_no,
send_date=@send_date,
report_date=@report_date,
conclusion=@conclusion,
testing_org=@testing_org,
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
            const string sql = "DELETE FROM type_inspection_records WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, TypeInspectionRecord record)
        {
            cmd.Parameters.AddWithValue("@inspection_id", (record.InspectionId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@product_id", (record.ProductId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@batch_no", (record.BatchNo ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@send_date", ToDbDate(record.SendDate));
            cmd.Parameters.AddWithValue("@report_date", ToDbDate(record.ReportDate));
            cmd.Parameters.AddWithValue("@conclusion", string.IsNullOrWhiteSpace(record.Conclusion) ? "合格" : record.Conclusion.Trim());
            cmd.Parameters.AddWithValue("@testing_org", (record.TestingOrg ?? string.Empty).Trim());
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
