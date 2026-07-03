using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 编制文件服务
    /// </summary>
    public class DocumentFileService
    {
        public List<DocumentFileRecord> ListAll()
        {
            const string sql = @"SELECT id, node_code, file_unique_id, department, std_category, std_level_1, std_level_2,
file_name, file_code, version, revision, revision_date, effective_date, file_link, is_invalid, remark,
created_at, updated_at
FROM document_files
ORDER BY id DESC";

            var result = new List<DocumentFileRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new DocumentFileRecord
                {
                    Id = GetLong(reader, "id"),
                    NodeCode = GetString(reader, "node_code"),
                    FileUniqueId = GetString(reader, "file_unique_id"),
                    Department = GetString(reader, "department"),
                    StdCategory = GetString(reader, "std_category"),
                    StdLevel1 = GetString(reader, "std_level_1"),
                    StdLevel2 = GetString(reader, "std_level_2"),
                    FileName = GetString(reader, "file_name"),
                    FileCode = GetString(reader, "file_code"),
                    Version = GetString(reader, "version"),
                    Revision = GetString(reader, "revision"),
                    RevisionDate = GetDate(reader, "revision_date"),
                    EffectiveDate = GetDate(reader, "effective_date"),
                    FileLink = GetString(reader, "file_link"),
                    IsInvalid = GetBool(reader, "is_invalid"),
                    Remark = GetString(reader, "remark"),
                    CreatedAt = GetDateTime(reader, "created_at"),
                    UpdatedAt = GetDateTime(reader, "updated_at")
                });
            }

            return result;
        }

        public long Insert(DocumentFileRecord record)
        {
            const string sql = @"INSERT INTO document_files
(node_code, file_unique_id, department, std_category, std_level_1, std_level_2, file_name, file_code,
 version, revision, revision_date, effective_date, file_link, is_invalid, remark)
VALUES
(@node_code, @file_unique_id, @department, @std_category, @std_level_1, @std_level_2, @file_name, @file_code,
 @version, @revision, @revision_date, @effective_date, @file_link, @is_invalid, @remark);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(DocumentFileRecord record)
        {
            const string sql = @"UPDATE document_files SET
node_code=@node_code,
file_unique_id=@file_unique_id,
department=@department,
std_category=@std_category,
std_level_1=@std_level_1,
std_level_2=@std_level_2,
file_name=@file_name,
file_code=@file_code,
version=@version,
revision=@revision,
revision_date=@revision_date,
effective_date=@effective_date,
file_link=@file_link,
is_invalid=@is_invalid,
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
            const string sql = "DELETE FROM document_files WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, DocumentFileRecord record)
        {
            cmd.Parameters.AddWithValue("@node_code", (record.NodeCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@file_unique_id", (record.FileUniqueId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@department", (record.Department ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@std_category", (record.StdCategory ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@std_level_1", (record.StdLevel1 ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@std_level_2", (record.StdLevel2 ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@file_name", (record.FileName ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@file_code", (record.FileCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@version", (record.Version ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@revision", (record.Revision ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@revision_date", ToDbDate(record.RevisionDate));
            cmd.Parameters.AddWithValue("@effective_date", ToDbDate(record.EffectiveDate));
            cmd.Parameters.AddWithValue("@file_link", (record.FileLink ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@is_invalid", record.IsInvalid ? 1 : 0);
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

        private static bool GetBool(MySqlDataReader reader, string field)
        {
            var text = GetString(reader, field);
            return text == "1" || bool.TryParse(text, out var value) && value;
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

        private static DateTime? GetDateTime(MySqlDataReader reader, string field)
        {
            var text = GetString(reader, field);
            if (string.IsNullOrWhiteSpace(text) || text.StartsWith("0000-00-00"))
            {
                return null;
            }

            return DateTime.TryParse(text, out var value) ? value : null;
        }
    }
}
