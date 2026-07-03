using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 报告发放服务
    /// </summary>
    public class ReportDistributionService
    {
        public List<ReportDistributionRecord> ListAll()
        {
            const string sql = @"SELECT id, report_code, distribution_date, distributor, recipient, receive_date,
acceptor, is_received, accept_date, is_accepted, remarks
FROM report_distribution
ORDER BY id DESC";

            var result = new List<ReportDistributionRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new ReportDistributionRecord
                {
                    Id = GetLong(reader, "id"),
                    ReportCode = GetString(reader, "report_code"),
                    DistributionDate = GetDate(reader, "distribution_date"),
                    Distributor = GetString(reader, "distributor"),
                    Recipient = GetString(reader, "recipient"),
                    ReceiveDate = GetDate(reader, "receive_date"),
                    Acceptor = GetString(reader, "acceptor"),
                    IsReceived = GetInt(reader, "is_received") != 0,
                    AcceptDate = GetDate(reader, "accept_date"),
                    IsAccepted = GetInt(reader, "is_accepted") != 0,
                    Remarks = GetString(reader, "remarks")
                });
            }

            return result;
        }

        public long Insert(ReportDistributionRecord record)
        {
            const string sql = @"INSERT INTO report_distribution
(report_code, distribution_date, distributor, recipient, receive_date, acceptor, is_received, accept_date, is_accepted, remarks)
VALUES
(@report_code, @distribution_date, @distributor, @recipient, @receive_date, @acceptor, @is_received, @accept_date, @is_accepted, @remarks);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(ReportDistributionRecord record)
        {
            const string sql = @"UPDATE report_distribution SET
report_code=@report_code,
distribution_date=@distribution_date,
distributor=@distributor,
recipient=@recipient,
receive_date=@receive_date,
acceptor=@acceptor,
is_received=@is_received,
accept_date=@accept_date,
is_accepted=@is_accepted,
remarks=@remarks
WHERE id=@id";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            cmd.Parameters.AddWithValue("@id", record.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(long id)
        {
            const string sql = "DELETE FROM report_distribution WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, ReportDistributionRecord record)
        {
            cmd.Parameters.AddWithValue("@report_code", (record.ReportCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@distribution_date", ToDbDate(record.DistributionDate));
            cmd.Parameters.AddWithValue("@distributor", (record.Distributor ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@recipient", (record.Recipient ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@receive_date", ToDbDate(record.ReceiveDate));
            cmd.Parameters.AddWithValue("@acceptor", (record.Acceptor ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@is_received", record.IsReceived ? 1 : 0);
            cmd.Parameters.AddWithValue("@accept_date", ToDbDate(record.AcceptDate));
            cmd.Parameters.AddWithValue("@is_accepted", record.IsAccepted ? 1 : 0);
            cmd.Parameters.AddWithValue("@remarks", (record.Remarks ?? string.Empty).Trim());
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
