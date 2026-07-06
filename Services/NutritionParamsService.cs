using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 营养参数服务
    /// </summary>
    public class NutritionParamsService
    {
        public List<NutritionParamsRecord> ListAll()
        {
            const string sql = @"SELECT id, sort_order, nutrient, unit, rounding_interval, zero_threshold, energy_value,
lower_error, upper_error, nrv, core, heavy_metal, is_sports_nutrition, daily_usage_range, disabled,
description, updated_at
FROM nutrient_parameters
ORDER BY sort_order ASC, id DESC";

            var result = new List<NutritionParamsRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new NutritionParamsRecord
                {
                    Id = GetLong(reader, "id"),
                    SortOrder = GetInt(reader, "sort_order"),
                    Nutrient = GetString(reader, "nutrient"),
                    Unit = GetString(reader, "unit"),
                    RoundingInterval = GetDecimal(reader, "rounding_interval"),
                    ZeroThreshold = GetDecimal(reader, "zero_threshold"),
                    EnergyValue = GetDecimal(reader, "energy_value"),
                    LowerError = GetDecimal(reader, "lower_error"),
                    UpperError = GetDecimal(reader, "upper_error"),
                    Nrv = GetDecimal(reader, "nrv"),
                    Core = GetBool(reader, "core"),
                    HeavyMetal = GetBool(reader, "heavy_metal"),
                    IsSportsNutrition = GetBool(reader, "is_sports_nutrition"),
                    DailyUsageRange = GetString(reader, "daily_usage_range"),
                    Disabled = GetBool(reader, "disabled"),
                    Description = GetString(reader, "description"),
                    UpdatedAt = GetDate(reader, "updated_at")
                });
            }

            return result;
        }

        public long Insert(NutritionParamsRecord record)
        {
            const string sql = @"INSERT INTO nutrient_parameters
(sort_order, nutrient, unit, rounding_interval, zero_threshold, energy_value, lower_error, upper_error,
 nrv, core, heavy_metal, is_sports_nutrition, daily_usage_range, disabled, description)
VALUES
(@sort_order, @nutrient, @unit, @rounding_interval, @zero_threshold, @energy_value, @lower_error, @upper_error,
 @nrv, @core, @heavy_metal, @is_sports_nutrition, @daily_usage_range, @disabled, @description);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(NutritionParamsRecord record)
        {
            const string sql = @"UPDATE nutrient_parameters SET
sort_order=@sort_order,
nutrient=@nutrient,
unit=@unit,
rounding_interval=@rounding_interval,
zero_threshold=@zero_threshold,
energy_value=@energy_value,
lower_error=@lower_error,
upper_error=@upper_error,
nrv=@nrv,
core=@core,
heavy_metal=@heavy_metal,
is_sports_nutrition=@is_sports_nutrition,
daily_usage_range=@daily_usage_range,
disabled=@disabled,
description=@description
WHERE id=@id";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            cmd.Parameters.AddWithValue("@id", record.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(long id)
        {
            const string sql = "DELETE FROM nutrient_parameters WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, NutritionParamsRecord record)
        {
            cmd.Parameters.AddWithValue("@sort_order", record.SortOrder);
            cmd.Parameters.AddWithValue("@nutrient", (record.Nutrient ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@unit", (record.Unit ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@rounding_interval", ToDbNumber(record.RoundingInterval));
            cmd.Parameters.AddWithValue("@zero_threshold", ToDbNumber(record.ZeroThreshold));
            cmd.Parameters.AddWithValue("@energy_value", ToDbNumber(record.EnergyValue));
            cmd.Parameters.AddWithValue("@lower_error", ToDbNumber(record.LowerError));
            cmd.Parameters.AddWithValue("@upper_error", ToDbNumber(record.UpperError));
            cmd.Parameters.AddWithValue("@nrv", ToDbNumber(record.Nrv));
            cmd.Parameters.AddWithValue("@core", record.Core ? 1 : 0);
            cmd.Parameters.AddWithValue("@heavy_metal", record.HeavyMetal ? 1 : 0);
            cmd.Parameters.AddWithValue("@is_sports_nutrition", record.IsSportsNutrition ? 1 : 0);
            cmd.Parameters.AddWithValue("@daily_usage_range", (record.DailyUsageRange ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@disabled", record.Disabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@description", (record.Description ?? string.Empty).Trim());
        }

        private static object ToDbNumber(decimal? value)
        {
            return value.HasValue ? value.Value : DBNull.Value;
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

        private static bool GetBool(MySqlDataReader reader, string field)
        {
            var text = GetString(reader, field);
            return text == "1" || bool.TryParse(text, out var value) && value;
        }

        private static decimal? GetDecimal(MySqlDataReader reader, string field)
        {
            var text = GetString(reader, field);
            return decimal.TryParse(text, out var value) ? value : null;
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
