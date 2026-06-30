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
    /// 取样记录业务服务
    /// </summary>
    public class SamplingRecordService
    {
        private static readonly Random _rnd = new Random();

        private static string GetConnString()
        {
            var cfg = MysqlDbInitializer.LoadMysqlConfig();
            return $"server={cfg.Host};port={cfg.Port};user={cfg.User};password={cfg.Password};database={cfg.Database};charset=utf8mb4;Pooling=true;Max Pool Size=10;Min Pool Size=1";
        }

        #region 查询
        /// <summary>
        /// 查询全部未删除记录，可按节点编号筛选
        /// </summary>
        public List<SamplingRecord> ListByNodeCode(string? nodeCode = null)
        {
            var list = new List<SamplingRecord>();
            using var conn = new MySqlConnection(GetConnString());
            conn.Open();

            var sql = @"
SELECT id, sampling_id, node_code, sampling_date, inspection_date, sample_name, sample_batch,
       sampling_quantity, representative_quantity, sample_source, sampler, brand_series, remark, is_deleted
FROM qa_sampling_records
WHERE is_deleted = 0";
            var pars = new List<MySqlParameter>();
            if (!string.IsNullOrWhiteSpace(nodeCode))
            {
                sql += " AND node_code = @nodeCode";
                pars.Add(new MySqlParameter("@nodeCode", nodeCode));
            }
            sql += " ORDER BY sampling_date DESC, id DESC";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddRange(pars.ToArray());
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapRecord(reader));
            }
            return list;
        }

        /// <summary>
        /// 按主键查询
        /// </summary>
        public SamplingRecord? GetById(long id)
        {
            using var conn = new MySqlConnection(GetConnString());
            conn.Open();
            const string sql = @"
SELECT id, sampling_id, node_code, sampling_date, inspection_date, sample_name, sample_batch,
       sampling_quantity, representative_quantity, sample_source, sampler, brand_series, remark, is_deleted
FROM qa_sampling_records
WHERE id = @id AND is_deleted = 0";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add(new MySqlParameter("@id", id));
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapRecord(reader) : null;
        }
        #endregion

        #region 增改删
        /// <summary>
        /// 新增记录
        /// </summary>
        public long Insert(SamplingRecord record)
        {
            using var conn = new MySqlConnection(GetConnString());
            conn.Open();

            if (string.IsNullOrWhiteSpace(record.SamplingId))
            {
                record.SamplingId = GenerateSamplingId(conn);
            }

            const string sql = @"
INSERT INTO qa_sampling_records
(sampling_id, node_code, sampling_date, inspection_date, sample_name, sample_batch,
 sampling_quantity, representative_quantity, sample_source, sampler, brand_series, remark, is_deleted)
VALUES
(@sampling_id, @node_code, @sampling_date, @inspection_date, @sample_name, @sample_batch,
 @sampling_quantity, @representative_quantity, @sample_source, @sampler, @brand_series, @remark, 0);
SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            AddParameters(cmd, record);
            var result = cmd.ExecuteScalar();
            return Convert.ToInt64(result);
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        public bool Update(SamplingRecord record)
        {
            if (record.Id <= 0) return false;
            using var conn = new MySqlConnection(GetConnString());
            conn.Open();
            const string sql = @"
UPDATE qa_sampling_records
SET sampling_id = @sampling_id,
    node_code = @node_code,
    sampling_date = @sampling_date,
    inspection_date = @inspection_date,
    sample_name = @sample_name,
    sample_batch = @sample_batch,
    sampling_quantity = @sampling_quantity,
    representative_quantity = @representative_quantity,
    sample_source = @sample_source,
    sampler = @sampler,
    brand_series = @brand_series,
    remark = @remark
WHERE id = @id AND is_deleted = 0";
            using var cmd = new MySqlCommand(sql, conn);
            AddParameters(cmd, record);
            cmd.Parameters.Add(new MySqlParameter("@id", record.Id));
            return cmd.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// 软删除记录
        /// </summary>
        public bool Delete(long id)
        {
            using var conn = new MySqlConnection(GetConnString());
            conn.Open();
            const string sql = "UPDATE qa_sampling_records SET is_deleted = 1 WHERE id = @id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add(new MySqlParameter("@id", id));
            return cmd.ExecuteNonQuery() > 0;
        }
        #endregion

        #region 编号生成
        /// <summary>
        /// 生成唯一取样编号
        /// </summary>
        public string GenerateSamplingId()
        {
            using var conn = new MySqlConnection(GetConnString());
            conn.Open();
            return GenerateSamplingId(conn);
        }

        private string GenerateSamplingId(MySqlConnection conn)
        {
            const string prefix = "SY";
            var datePart = DateTime.Now.ToString("yyyyMMddHHmmss");
            while (true)
            {
                var id = $"{prefix}{datePart}{_rnd.Next(1000, 10000)}";
                using var cmd = new MySqlCommand("SELECT 1 FROM qa_sampling_records WHERE sampling_id = @id LIMIT 1", conn);
                cmd.Parameters.Add(new MySqlParameter("@id", id));
                if (cmd.ExecuteScalar() == null)
                    return id;
            }
        }
        #endregion

        #region CSV 导入导出
        /// <summary>
        /// 从 CSV 导入取样记录
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
                    var record = new SamplingRecord();
                    record.SamplingId = GetValue(values, map, "sampling_id");
                    record.NodeCode = GetValue(values, map, "node_code");
                    record.SamplingDate = ParseDate(GetValue(values, map, "sampling_date"));
                    record.InspectionDate = ParseDate(GetValue(values, map, "inspection_date"));
                    record.SampleName = GetValue(values, map, "sample_name");
                    record.SampleBatch = GetValue(values, map, "sample_batch");
                    record.BrandSeries = GetValue(values, map, "brand_series");
                    record.SamplingQuantity = GetValue(values, map, "sampling_quantity");
                    record.RepresentativeQuantity = GetValue(values, map, "representative_quantity");
                    record.SampleSource = GetValue(values, map, "sample_source");
                    record.Sampler = GetValue(values, map, "sampler");
                    record.Remark = GetValue(values, map, "remark");

                    if (string.IsNullOrWhiteSpace(record.SamplingId))
                        record.SamplingId = GenerateSamplingId(conn);

                    const string sql = @"
INSERT INTO qa_sampling_records
(sampling_id, node_code, sampling_date, inspection_date, sample_name, sample_batch,
 sampling_quantity, representative_quantity, sample_source, sampler, brand_series, remark, is_deleted)
VALUES
(@sampling_id, @node_code, @sampling_date, @inspection_date, @sample_name, @sample_batch,
 @sampling_quantity, @representative_quantity, @sample_source, @sampler, @brand_series, @remark, 0)";
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
        /// 导出取样记录到 CSV
        /// </summary>
        public void ExportToCsv(string filePath, IEnumerable<SamplingRecord> records)
        {
            var sb = new StringBuilder();
            sb.AppendLine("取样ID,节点编号,取样日期,检验日期,检品名称,检品批号,品牌系列,取样量,代表量,检品来源,取样人,备注");
            foreach (var r in records)
            {
                sb.AppendLine($"{EscapeCsv(r.SamplingId)},{EscapeCsv(r.NodeCode)},{EscapeCsv(r.SamplingDate.ToString("yyyy-MM-dd"))},{EscapeCsv(r.InspectionDate.ToString("yyyy-MM-dd"))},{EscapeCsv(r.SampleName)},{EscapeCsv(r.SampleBatch)},{EscapeCsv(r.BrandSeries)},{EscapeCsv(r.SamplingQuantity)},{EscapeCsv(r.RepresentativeQuantity)},{EscapeCsv(r.SampleSource)},{EscapeCsv(r.Sampler)},{EscapeCsv(r.Remark)}");
            }
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
        #endregion

        #region 内部辅助
        private static SamplingRecord MapRecord(MySqlDataReader reader)
        {
            return new SamplingRecord
            {
                Id = reader.GetInt64("id"),
                SamplingId = reader.GetString("sampling_id"),
                NodeCode = reader.GetString("node_code"),
                SamplingDate = reader.GetDateTime("sampling_date"),
                InspectionDate = reader.GetDateTime("inspection_date"),
                SampleName = reader.GetString("sample_name"),
                SampleBatch = reader.GetString("sample_batch"),
                SamplingQuantity = reader.IsDBNull(reader.GetOrdinal("sampling_quantity")) ? string.Empty : reader.GetString("sampling_quantity"),
                RepresentativeQuantity = reader.IsDBNull(reader.GetOrdinal("representative_quantity")) ? string.Empty : reader.GetString("representative_quantity"),
                SampleSource = reader.GetString("sample_source"),
                Sampler = reader.GetString("sampler"),
                BrandSeries = reader.IsDBNull(reader.GetOrdinal("brand_series")) ? string.Empty : reader.GetString("brand_series"),
                Remark = reader.IsDBNull(reader.GetOrdinal("remark")) ? string.Empty : reader.GetString("remark"),
                IsDeleted = reader.GetBoolean("is_deleted")
            };
        }

        private static void AddParameters(MySqlCommand cmd, SamplingRecord record)
        {
            cmd.Parameters.Add(new MySqlParameter("@sampling_id", record.SamplingId));
            cmd.Parameters.Add(new MySqlParameter("@node_code", record.NodeCode));
            cmd.Parameters.Add(new MySqlParameter("@sampling_date", record.SamplingDate));
            cmd.Parameters.Add(new MySqlParameter("@inspection_date", record.InspectionDate));
            cmd.Parameters.Add(new MySqlParameter("@sample_name", record.SampleName));
            cmd.Parameters.Add(new MySqlParameter("@sample_batch", record.SampleBatch));
            cmd.Parameters.Add(new MySqlParameter("@sampling_quantity", string.IsNullOrEmpty(record.SamplingQuantity) ? (object)DBNull.Value : record.SamplingQuantity));
            cmd.Parameters.Add(new MySqlParameter("@representative_quantity", string.IsNullOrEmpty(record.RepresentativeQuantity) ? (object)DBNull.Value : record.RepresentativeQuantity));
            cmd.Parameters.Add(new MySqlParameter("@sample_source", record.SampleSource));
            cmd.Parameters.Add(new MySqlParameter("@sampler", record.Sampler));
            cmd.Parameters.Add(new MySqlParameter("@brand_series", string.IsNullOrEmpty(record.BrandSeries) ? (object)DBNull.Value : record.BrandSeries));
            cmd.Parameters.Add(new MySqlParameter("@remark", string.IsNullOrEmpty(record.Remark) ? (object)DBNull.Value : record.Remark));
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
