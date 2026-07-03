using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 营养标签服务
    /// </summary>
    public class NutritionLabelService
    {
        public List<NutritionLabelRecord> ListAll()
        {
            const string sql = @"SELECT label_id, name, detection_mode, heavy_metal, claim_types, remark, nutrient_data
FROM nutrition_labels
ORDER BY label_id DESC";

            var result = new List<NutritionLabelRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new NutritionLabelRecord
                {
                    LabelId = GetString(reader, "label_id"),
                    Name = GetString(reader, "name"),
                    DetectionMode = GetString(reader, "detection_mode"),
                    HeavyMetal = GetBool(reader, "heavy_metal"),
                    ClaimTypes = GetString(reader, "claim_types"),
                    Remark = GetString(reader, "remark"),
                    NutrientData = GetString(reader, "nutrient_data")
                });
            }

            return result;
        }

        public void Insert(NutritionLabelRecord record)
        {
            const string sql = @"INSERT INTO nutrition_labels
(label_id, name, detection_mode, heavy_metal, claim_types, remark, nutrient_data, created_at, updated_at)
VALUES
(@label_id, @name, @detection_mode, @heavy_metal, @claim_types, @remark, @nutrient_data, NOW(), NOW())";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            cmd.ExecuteNonQuery();
        }

        public void Update(NutritionLabelRecord record)
        {
            const string sql = @"UPDATE nutrition_labels SET
name=@name,
detection_mode=@detection_mode,
heavy_metal=@heavy_metal,
claim_types=@claim_types,
remark=@remark,
nutrient_data=@nutrient_data,
updated_at=NOW()
WHERE label_id=@label_id";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            cmd.ExecuteNonQuery();
        }

        public void Delete(string labelId)
        {
            const string sql = "DELETE FROM nutrition_labels WHERE label_id=@label_id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@label_id", labelId);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, NutritionLabelRecord record)
        {
            cmd.Parameters.AddWithValue("@label_id", (record.LabelId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@name", (record.Name ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@detection_mode", string.IsNullOrWhiteSpace(record.DetectionMode) ? "core" : record.DetectionMode.Trim());
            cmd.Parameters.AddWithValue("@heavy_metal", record.HeavyMetal ? 1 : 0);
            cmd.Parameters.AddWithValue("@claim_types", (record.ClaimTypes ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@remark", (record.Remark ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@nutrient_data", (record.NutrientData ?? string.Empty).Trim());
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

        private static bool GetBool(MySqlDataReader reader, string field)
        {
            var text = GetString(reader, field);
            return text == "1" || bool.TryParse(text, out var v) && v;
        }
    }
}
