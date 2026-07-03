using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 考试配置服务
    /// </summary>
    public class ExamConfigService
    {
        public List<ExamConfigRecord> ListAll()
        {
            const string sql = @"SELECT id, config_id, node_code, exam_name, exam_type, department, total_score, pass_score,
duration_minutes, judge_count, judge_single_score, single_count, single_single_score, multi_count,
multi_single_score, essay_count, essay_single_score, category_filter, difficulty_filter, is_enabled,
remark, created_by, created_at, updated_at
FROM exam_config
ORDER BY id DESC";

            var result = new List<ExamConfigRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new ExamConfigRecord
                {
                    Id = GetLong(reader, "id"),
                    ConfigId = GetString(reader, "config_id"),
                    NodeCode = GetString(reader, "node_code"),
                    ExamName = GetString(reader, "exam_name"),
                    ExamType = GetString(reader, "exam_type"),
                    Department = GetString(reader, "department"),
                    TotalScore = GetInt(reader, "total_score"),
                    PassScore = GetInt(reader, "pass_score"),
                    DurationMinutes = GetInt(reader, "duration_minutes"),
                    JudgeCount = GetInt(reader, "judge_count"),
                    JudgeSingleScore = GetDecimal(reader, "judge_single_score") ?? 0m,
                    SingleCount = GetInt(reader, "single_count"),
                    SingleSingleScore = GetDecimal(reader, "single_single_score") ?? 0m,
                    MultiCount = GetInt(reader, "multi_count"),
                    MultiSingleScore = GetDecimal(reader, "multi_single_score") ?? 0m,
                    EssayCount = GetInt(reader, "essay_count"),
                    EssaySingleScore = GetDecimal(reader, "essay_single_score") ?? 0m,
                    CategoryFilter = GetString(reader, "category_filter"),
                    DifficultyFilter = GetString(reader, "difficulty_filter"),
                    IsEnabled = GetBool(reader, "is_enabled"),
                    Remark = GetString(reader, "remark"),
                    CreatedBy = GetString(reader, "created_by"),
                    CreatedAt = GetDate(reader, "created_at"),
                    UpdatedAt = GetDate(reader, "updated_at")
                });
            }

            return result;
        }

        public long Insert(ExamConfigRecord record)
        {
            const string sql = @"INSERT INTO exam_config
(config_id, node_code, exam_name, exam_type, department, total_score, pass_score, duration_minutes,
 judge_count, judge_single_score, single_count, single_single_score, multi_count, multi_single_score,
 essay_count, essay_single_score, category_filter, difficulty_filter, is_enabled, remark, created_by)
VALUES
(@config_id, @node_code, @exam_name, @exam_type, @department, @total_score, @pass_score, @duration_minutes,
 @judge_count, @judge_single_score, @single_count, @single_single_score, @multi_count, @multi_single_score,
 @essay_count, @essay_single_score, @category_filter, @difficulty_filter, @is_enabled, @remark, @created_by);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(ExamConfigRecord record)
        {
            const string sql = @"UPDATE exam_config SET
config_id=@config_id,
node_code=@node_code,
exam_name=@exam_name,
exam_type=@exam_type,
department=@department,
total_score=@total_score,
pass_score=@pass_score,
duration_minutes=@duration_minutes,
judge_count=@judge_count,
judge_single_score=@judge_single_score,
single_count=@single_count,
single_single_score=@single_single_score,
multi_count=@multi_count,
multi_single_score=@multi_single_score,
essay_count=@essay_count,
essay_single_score=@essay_single_score,
category_filter=@category_filter,
difficulty_filter=@difficulty_filter,
is_enabled=@is_enabled,
remark=@remark,
created_by=@created_by
WHERE id=@id";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            cmd.Parameters.AddWithValue("@id", record.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(long id)
        {
            const string sql = "DELETE FROM exam_config WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, ExamConfigRecord record)
        {
            cmd.Parameters.AddWithValue("@config_id", (record.ConfigId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@node_code", (record.NodeCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@exam_name", (record.ExamName ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@exam_type", (record.ExamType ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@department", (record.Department ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@total_score", record.TotalScore);
            cmd.Parameters.AddWithValue("@pass_score", record.PassScore);
            cmd.Parameters.AddWithValue("@duration_minutes", record.DurationMinutes);
            cmd.Parameters.AddWithValue("@judge_count", record.JudgeCount);
            cmd.Parameters.AddWithValue("@judge_single_score", record.JudgeSingleScore);
            cmd.Parameters.AddWithValue("@single_count", record.SingleCount);
            cmd.Parameters.AddWithValue("@single_single_score", record.SingleSingleScore);
            cmd.Parameters.AddWithValue("@multi_count", record.MultiCount);
            cmd.Parameters.AddWithValue("@multi_single_score", record.MultiSingleScore);
            cmd.Parameters.AddWithValue("@essay_count", record.EssayCount);
            cmd.Parameters.AddWithValue("@essay_single_score", record.EssaySingleScore);
            cmd.Parameters.AddWithValue("@category_filter", (record.CategoryFilter ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@difficulty_filter", (record.DifficultyFilter ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@is_enabled", record.IsEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@remark", (record.Remark ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@created_by", (record.CreatedBy ?? string.Empty).Trim());
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

        private static decimal? GetDecimal(MySqlDataReader reader, string field)
        {
            var text = GetString(reader, field);
            return decimal.TryParse(text, out var value) ? value : null;
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
    }
}
