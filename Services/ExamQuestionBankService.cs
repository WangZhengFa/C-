using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 考试题库服务
    /// </summary>
    public class ExamQuestionBankService
    {
        public List<ExamQuestionBankRecord> ListAll()
        {
            const string sql = @"SELECT id, question_type, question_content, options_json, answer, analysis,
category, difficulty, is_enabled, remark
FROM exam_question_bank
ORDER BY id DESC";

            var result = new List<ExamQuestionBankRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new ExamQuestionBankRecord
                {
                    Id = GetLong(reader, "id"),
                    QuestionType = GetString(reader, "question_type"),
                    QuestionContent = GetString(reader, "question_content"),
                    OptionsJson = GetString(reader, "options_json"),
                    Answer = GetString(reader, "answer"),
                    Analysis = GetString(reader, "analysis"),
                    Category = GetString(reader, "category"),
                    Difficulty = GetString(reader, "difficulty"),
                    IsEnabled = GetBool(reader, "is_enabled"),
                    Remark = GetString(reader, "remark")
                });
            }

            return result;
        }

        public long Insert(ExamQuestionBankRecord record)
        {
            const string sql = @"INSERT INTO exam_question_bank
(question_type, question_content, options_json, answer, analysis, category, difficulty, is_enabled, remark)
VALUES
(@question_type, @question_content, @options_json, @answer, @analysis, @category, @difficulty, @is_enabled, @remark);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(ExamQuestionBankRecord record)
        {
            const string sql = @"UPDATE exam_question_bank SET
question_type=@question_type,
question_content=@question_content,
options_json=@options_json,
answer=@answer,
analysis=@analysis,
category=@category,
difficulty=@difficulty,
is_enabled=@is_enabled,
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
            const string sql = "DELETE FROM exam_question_bank WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, ExamQuestionBankRecord record)
        {
            cmd.Parameters.AddWithValue("@question_type", string.IsNullOrWhiteSpace(record.QuestionType) ? "单选题" : record.QuestionType.Trim());
            cmd.Parameters.AddWithValue("@question_content", (record.QuestionContent ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@options_json", (record.OptionsJson ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@answer", (record.Answer ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@analysis", (record.Analysis ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@category", (record.Category ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@difficulty", string.IsNullOrWhiteSpace(record.Difficulty) ? "中等" : record.Difficulty.Trim());
            cmd.Parameters.AddWithValue("@is_enabled", record.IsEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@remark", (record.Remark ?? string.Empty).Trim());
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
    }
}
