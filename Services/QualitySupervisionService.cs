using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MySqlConnector;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 质量监督业务服务
    /// </summary>
    public class QualitySupervisionService
    {
        private static readonly Random _rnd = new Random();

        private static string GetConnString()
        {
            var cfg = MysqlDbInitializer.LoadMysqlConfig();
            return $"server={cfg.Host};port={cfg.Port};user={cfg.User};password={cfg.Password};database={cfg.Database};charset=utf8mb4;Pooling=true;Max Pool Size=10;Min Pool Size=1";
        }

        #region 查询
        /// <summary>
        /// 查询全部质量监督记录
        /// </summary>
        public List<QualitySupervision> ListAll()
        {
            var list = new List<QualitySupervision>();
            using var conn = new MySqlConnection(GetConnString());
            conn.Open();
            const string sql = @"
SELECT supervision_id, discovery_date, project_category, project_name, batch_number, quantity,
       non_compliance, rectification_actions, rectification_deadline, rectification_result,
       supervisor, is_reviewed, remarks
FROM qa_supervision
ORDER BY discovery_date DESC, supervision_id DESC";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapRecord(reader));
            }
            return list;
        }

        /// <summary>
        /// 按编号查询
        /// </summary>
        public QualitySupervision? GetById(string supervisionId)
        {
            using var conn = new MySqlConnection(GetConnString());
            conn.Open();
            const string sql = @"
SELECT supervision_id, discovery_date, project_category, project_name, batch_number, quantity,
       non_compliance, rectification_actions, rectification_deadline, rectification_result,
       supervisor, is_reviewed, remarks
FROM qa_supervision
WHERE supervision_id = @id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add(new MySqlParameter("@id", supervisionId));
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapRecord(reader) : null;
        }
        #endregion

        #region 增改删
        /// <summary>
        /// 新增记录
        /// </summary>
        public void Insert(QualitySupervision record)
        {
            using var conn = new MySqlConnection(GetConnString());
            conn.Open();
            if (string.IsNullOrWhiteSpace(record.SupervisionId))
                record.SupervisionId = GenerateSupervisionId(conn);

            const string sql = @"
INSERT INTO qa_supervision
(supervision_id, discovery_date, project_category, project_name, batch_number, quantity,
 non_compliance, rectification_actions, rectification_deadline, rectification_result,
 supervisor, is_reviewed, remarks)
VALUES
(@id, @discovery_date, @project_category, @project_name, @batch_number, @quantity,
 @non_compliance, @rectification_actions, @rectification_deadline, @rectification_result,
 @supervisor, @is_reviewed, @remarks)";
            using var cmd = new MySqlCommand(sql, conn);
            AddParameters(cmd, record);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        public bool Update(QualitySupervision record)
        {
            if (string.IsNullOrWhiteSpace(record.SupervisionId)) return false;
            using var conn = new MySqlConnection(GetConnString());
            conn.Open();
            const string sql = @"
UPDATE qa_supervision
SET discovery_date = @discovery_date,
    project_category = @project_category,
    project_name = @project_name,
    batch_number = @batch_number,
    quantity = @quantity,
    non_compliance = @non_compliance,
    rectification_actions = @rectification_actions,
    rectification_deadline = @rectification_deadline,
    rectification_result = @rectification_result,
    supervisor = @supervisor,
    is_reviewed = @is_reviewed,
    remarks = @remarks
WHERE supervision_id = @id";
            using var cmd = new MySqlCommand(sql, conn);
            AddParameters(cmd, record);
            return cmd.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// 删除记录
        /// </summary>
        public bool Delete(string supervisionId)
        {
            using var conn = new MySqlConnection(GetConnString());
            conn.Open();
            const string sql = "DELETE FROM qa_supervision WHERE supervision_id = @id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add(new MySqlParameter("@id", supervisionId));
            return cmd.ExecuteNonQuery() > 0;
        }
        #endregion

        #region 编号生成
        public string GenerateSupervisionId()
        {
            using var conn = new MySqlConnection(GetConnString());
            conn.Open();
            return GenerateSupervisionId(conn);
        }

        private string GenerateSupervisionId(MySqlConnection conn)
        {
            var datePart = DateTime.Now.ToString("yyyyMMddHHmmss");
            while (true)
            {
                var id = $"QJ{datePart}{_rnd.Next(1000, 10000)}";
                using var cmd = new MySqlCommand("SELECT 1 FROM qa_supervision WHERE supervision_id = @id LIMIT 1", conn);
                cmd.Parameters.Add(new MySqlParameter("@id", id));
                if (cmd.ExecuteScalar() == null)
                    return id;
            }
        }
        #endregion

        #region CSV 导入导出
        /// <summary>
        /// 从 CSV 导入
        /// </summary>
        public (int success, int fail) ImportFromCsv(string filePath)
        {
            int success = 0, fail = 0;
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            if (lines.Length <= 1) return (success, fail);

            var header = ParseCsvLine(lines[0]);
            var map = BuildHeaderMap(header);

            using var conn = new MySqlConnection(GetConnString());
            conn.Open();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                try
                {
                    var values = ParseCsvLine(lines[i]);
                    var record = new QualitySupervision();
                    record.SupervisionId = GetValue(values, map, "supervision_id");
                    record.DiscoveryDate = ParseDate(GetValue(values, map, "discovery_date"));
                    record.ProjectCategory = GetValue(values, map, "project_category");
                    record.ProjectName = GetValue(values, map, "project_name");
                    record.BatchNumber = GetValue(values, map, "batch_number");
                    record.Quantity = GetValue(values, map, "quantity");
                    record.NonCompliance = GetValue(values, map, "non_compliance");
                    record.RectificationActions = GetValue(values, map, "rectification_actions");
                    record.RectificationDeadline = ParseNullableDate(GetValue(values, map, "rectification_deadline"));
                    record.RectificationResult = GetValue(values, map, "rectification_result");
                    record.Supervisor = GetValue(values, map, "supervisor");
                    record.IsReviewed = GetValue(values, map, "is_reviewed") == "1" || GetValue(values, map, "is_reviewed") == "是";
                    record.Remarks = GetValue(values, map, "remarks");

                    if (string.IsNullOrWhiteSpace(record.SupervisionId))
                        record.SupervisionId = GenerateSupervisionId(conn);

                    const string sql = @"
INSERT INTO qa_supervision
(supervision_id, discovery_date, project_category, project_name, batch_number, quantity,
 non_compliance, rectification_actions, rectification_deadline, rectification_result,
 supervisor, is_reviewed, remarks)
VALUES
(@id, @discovery_date, @project_category, @project_name, @batch_number, @quantity,
 @non_compliance, @rectification_actions, @rectification_deadline, @rectification_result,
 @supervisor, @is_reviewed, @remarks)";
                    using var cmd = new MySqlCommand(sql, conn);
                    AddParameters(cmd, record);
                    cmd.ExecuteNonQuery();
                    success++;
                }
                catch
                {
                    fail++;
                }
            }
            return (success, fail);
        }

        /// <summary>
        /// 导出到 CSV
        /// </summary>
        public void ExportToCsv(string filePath, IEnumerable<QualitySupervision> records)
        {
            var sb = new StringBuilder();
            sb.AppendLine("监督ID,发现日期,项目类别,项目名称,批号,数量,不符合项,整改措施,整改期限,整改结果,监督人,已审核,备注");
            foreach (var r in records)
            {
                sb.AppendLine($"{EscapeCsv(r.SupervisionId)},{EscapeCsv(r.DiscoveryDate.ToString("yyyy-MM-dd"))},{EscapeCsv(r.ProjectCategory)},{EscapeCsv(r.ProjectName)},{EscapeCsv(r.BatchNumber)},{EscapeCsv(r.Quantity)},{EscapeCsv(r.NonCompliance)},{EscapeCsv(r.RectificationActions)},{EscapeCsv(r.RectificationDeadline?.ToString("yyyy-MM-dd") ?? "")},{EscapeCsv(r.RectificationResult)},{EscapeCsv(r.Supervisor)},{EscapeCsv(r.IsReviewed ? "是" : "否")},{EscapeCsv(r.Remarks)}");
            }
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
        #endregion

        #region 内部辅助
        private static QualitySupervision MapRecord(MySqlDataReader reader)
        {
            return new QualitySupervision
            {
                SupervisionId = reader.GetString("supervision_id"),
                DiscoveryDate = reader.GetDateTime("discovery_date"),
                ProjectCategory = reader.IsDBNull(reader.GetOrdinal("project_category")) ? string.Empty : reader.GetString("project_category"),
                ProjectName = reader.IsDBNull(reader.GetOrdinal("project_name")) ? string.Empty : reader.GetString("project_name"),
                BatchNumber = reader.IsDBNull(reader.GetOrdinal("batch_number")) ? string.Empty : reader.GetString("batch_number"),
                Quantity = reader.IsDBNull(reader.GetOrdinal("quantity")) ? string.Empty : reader.GetString("quantity"),
                NonCompliance = reader.IsDBNull(reader.GetOrdinal("non_compliance")) ? string.Empty : reader.GetString("non_compliance"),
                RectificationActions = reader.IsDBNull(reader.GetOrdinal("rectification_actions")) ? string.Empty : reader.GetString("rectification_actions"),
                RectificationDeadline = reader.IsDBNull(reader.GetOrdinal("rectification_deadline")) ? (DateTime?)null : reader.GetDateTime("rectification_deadline"),
                RectificationResult = reader.IsDBNull(reader.GetOrdinal("rectification_result")) ? string.Empty : reader.GetString("rectification_result"),
                Supervisor = reader.IsDBNull(reader.GetOrdinal("supervisor")) ? string.Empty : reader.GetString("supervisor"),
                IsReviewed = reader.GetBoolean("is_reviewed"),
                Remarks = reader.IsDBNull(reader.GetOrdinal("remarks")) ? string.Empty : reader.GetString("remarks")
            };
        }

        private static void AddParameters(MySqlCommand cmd, QualitySupervision record)
        {
            cmd.Parameters.Add(new MySqlParameter("@id", record.SupervisionId));
            cmd.Parameters.Add(new MySqlParameter("@discovery_date", record.DiscoveryDate));
            cmd.Parameters.Add(new MySqlParameter("@project_category", string.IsNullOrEmpty(record.ProjectCategory) ? (object)DBNull.Value : record.ProjectCategory));
            cmd.Parameters.Add(new MySqlParameter("@project_name", string.IsNullOrEmpty(record.ProjectName) ? (object)DBNull.Value : record.ProjectName));
            cmd.Parameters.Add(new MySqlParameter("@batch_number", string.IsNullOrEmpty(record.BatchNumber) ? (object)DBNull.Value : record.BatchNumber));
            cmd.Parameters.Add(new MySqlParameter("@quantity", string.IsNullOrEmpty(record.Quantity) ? (object)DBNull.Value : record.Quantity));
            cmd.Parameters.Add(new MySqlParameter("@non_compliance", string.IsNullOrEmpty(record.NonCompliance) ? (object)DBNull.Value : record.NonCompliance));
            cmd.Parameters.Add(new MySqlParameter("@rectification_actions", string.IsNullOrEmpty(record.RectificationActions) ? (object)DBNull.Value : record.RectificationActions));
            cmd.Parameters.Add(new MySqlParameter("@rectification_deadline", record.RectificationDeadline.HasValue ? (object)record.RectificationDeadline.Value : DBNull.Value));
            cmd.Parameters.Add(new MySqlParameter("@rectification_result", string.IsNullOrEmpty(record.RectificationResult) ? (object)DBNull.Value : record.RectificationResult));
            cmd.Parameters.Add(new MySqlParameter("@supervisor", string.IsNullOrEmpty(record.Supervisor) ? (object)DBNull.Value : record.Supervisor));
            cmd.Parameters.Add(new MySqlParameter("@is_reviewed", record.IsReviewed));
            cmd.Parameters.Add(new MySqlParameter("@remarks", string.IsNullOrEmpty(record.Remarks) ? (object)DBNull.Value : record.Remarks));
        }

        private static Dictionary<string, int> BuildHeaderMap(List<string> header)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Count; i++)
            {
                var key = header[i].Trim().Replace("\uFEFF", "");
                map[key] = i;
            }
            return map;
        }

        private static string GetValue(List<string> values, Dictionary<string, int> map, string key)
        {
            if (map.TryGetValue(key, out var idx) && idx < values.Count)
                return values[idx].Trim();
            return string.Empty;
        }

        private static DateTime ParseDate(string value)
        {
            if (DateTime.TryParseExact(value, new[] { "yyyy-MM-dd", "yyyy/MM/dd", "yyyyMMdd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
            return DateTime.Today;
        }

        private static DateTime? ParseNullableDate(string value)
        {
            if (DateTime.TryParseExact(value, new[] { "yyyy-MM-dd", "yyyy/MM/dd", "yyyyMMdd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
            return null;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
            result.Add(sb.ToString());
            return result;
        }

        private static string EscapeCsv(string? value)
        {
            if (value == null) return "\"\"";
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return "\"" + value + "\"";
        }
        #endregion
    }
}
