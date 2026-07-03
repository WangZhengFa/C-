using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 食品分类服务
    /// </summary>
    public class FoodCategoryService
    {
        public List<FoodCategoryRecord> ListAll()
        {
            const string sql = @"SELECT id, category_code, category_name, parent_code, description, sort_order, is_enabled
FROM food_categories
ORDER BY sort_order ASC, id DESC";

            var result = new List<FoodCategoryRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new FoodCategoryRecord
                {
                    Id = GetLong(reader, "id"),
                    CategoryCode = GetString(reader, "category_code"),
                    CategoryName = GetString(reader, "category_name"),
                    ParentCode = GetString(reader, "parent_code"),
                    Description = GetString(reader, "description"),
                    SortOrder = GetInt(reader, "sort_order"),
                    IsEnabled = GetBool(reader, "is_enabled")
                });
            }

            return result;
        }

        public long Insert(FoodCategoryRecord record)
        {
            const string sql = @"INSERT INTO food_categories
(category_code, category_name, parent_code, description, sort_order, is_enabled)
VALUES
(@category_code, @category_name, @parent_code, @description, @sort_order, @is_enabled);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(FoodCategoryRecord record)
        {
            const string sql = @"UPDATE food_categories SET
category_code=@category_code,
category_name=@category_name,
parent_code=@parent_code,
description=@description,
sort_order=@sort_order,
is_enabled=@is_enabled
WHERE id=@id";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            cmd.Parameters.AddWithValue("@id", record.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(long id)
        {
            const string sql = "DELETE FROM food_categories WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, FoodCategoryRecord record)
        {
            cmd.Parameters.AddWithValue("@category_code", (record.CategoryCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@category_name", (record.CategoryName ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@parent_code", (record.ParentCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@description", (record.Description ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sort_order", record.SortOrder);
            cmd.Parameters.AddWithValue("@is_enabled", record.IsEnabled ? 1 : 0);
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
    }
}
