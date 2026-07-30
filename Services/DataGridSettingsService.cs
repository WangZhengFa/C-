using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 数据表格列设置持久化服务（MySQL 主存储 + 本地 JSON 降级）
    /// </summary>
    public class DataGridSettingsService
    {
        private static readonly string FallbackDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "ColumnSettings");

        private static string BuildConnString()
        {
            var cfg = MysqlDbInitializer.LoadMysqlConfig();
            return $"server={cfg.Host};port={cfg.Port};user={cfg.User};password={cfg.Password};database={cfg.Database};charset=utf8mb4;Pooling=true;Max Pool Size=10;Min Pool Size=1";
        }

        static DataGridSettingsService()
        {
            try
            {
                EnsureSchema();
            }
            catch
            {
                // 数据库不可用时不阻断界面，后续会降级到本地文件
            }
        }

        /// <summary>
        /// 确保 column_settings 表包含新版字段
        /// </summary>
        private static void EnsureSchema()
        {
            using var conn = new MySqlConnection(BuildConnString());
            conn.Open();
            var alters = new[]
            {
                "ALTER TABLE `column_settings` ADD COLUMN IF NOT EXISTS `font_size` INT DEFAULT 12",
                "ALTER TABLE `column_settings` ADD COLUMN IF NOT EXISTS `max_display_rows` INT DEFAULT 0",
            };
            foreach (var sql in alters)
            {
                try
                {
                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    // 忽略已存在或其他异常
                }
            }
        }

        public DataGridSettings? Load(string group)
        {
            if (string.IsNullOrWhiteSpace(group)) return null;

            try
            {
                using var conn = new MySqlConnection(BuildConnString());
                conn.Open();
                const string sql = @"
SELECT `hidden_columns`, `column_order`, `column_width_settings`, `fixed_column_width_settings`,
       `row_height`, `font_size`, `table_height_enabled`, `table_height_mode`, `table_height`,
       `max_display_rows`, `left_columns`, `center_columns`, `right_columns`, `sort_specs`
FROM `column_settings`
WHERE `settings_group` = @group
ORDER BY `updated_at` DESC, `id` DESC
LIMIT 1";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@group", group);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return null;

                return new DataGridSettings
                {
                    SettingsGroup = group,
                    HiddenColumns = ParseStringList(reader["hidden_columns"]),
                    ColumnOrder = ParseStringList(reader["column_order"]),
                    WidthSettings = ParseStringDict(reader["column_width_settings"]),
                    FixedWidths = ParseIntDict(reader["fixed_column_width_settings"]),
                    RowHeight = reader["row_height"] is DBNull ? 22 : Convert.ToInt32(reader["row_height"]),
                    FontSize = reader["font_size"] is DBNull ? 12 : Convert.ToInt32(reader["font_size"]),
                    TableHeightEnabled = (reader["table_height_enabled"] is DBNull ? 0 : Convert.ToInt32(reader["table_height_enabled"])) == 1,
                    TableHeightMode = reader["table_height_mode"] is DBNull ? "" : reader["table_height_mode"].ToString()!,
                    TableHeight = reader["table_height"] is DBNull ? 300 : Convert.ToInt32(reader["table_height"]),
                    MaxDisplayRows = reader["max_display_rows"] is DBNull ? 0 : Convert.ToInt32(reader["max_display_rows"]),
                    LeftColumns = ParseStringList(reader["left_columns"]),
                    CenterColumns = ParseStringList(reader["center_columns"]),
                    RightColumns = ParseStringList(reader["right_columns"]),
                    SortSpecs = ParseSortSpecs(reader["sort_specs"]),
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DataGridSettingsService] 从数据库加载失败: {ex.Message}");
                return LoadFromFallback(group);
            }
        }

        public bool Save(DataGridSettings settings)
        {
            if (settings == null || string.IsNullOrWhiteSpace(settings.SettingsGroup))
                return false;

            try
            {
                using var conn = new MySqlConnection(BuildConnString());
                conn.Open();

                const string updateSql = @"
UPDATE `column_settings` SET
    `hidden_columns` = @hidden,
    `column_order` = @order,
    `column_width_settings` = @width,
    `fixed_column_width_settings` = @fixed,
    `row_height` = @rowHeight,
    `font_size` = @fontSize,
    `table_height_enabled` = @thEnabled,
    `table_height_mode` = @thMode,
    `table_height` = @thHeight,
    `max_display_rows` = @maxRows,
    `left_columns` = @left,
    `center_columns` = @center,
    `right_columns` = @right,
    `sort_specs` = @sort,
    `updated_at` = CURRENT_TIMESTAMP
WHERE `id` = (
    SELECT `id` FROM (
        SELECT `id` FROM `column_settings` WHERE `settings_group` = @group
        ORDER BY `updated_at` DESC, `id` DESC LIMIT 1
    ) AS t
)";

                using (var cmd = new MySqlCommand(updateSql, conn))
                {
                    AddParameters(cmd, settings);
                    var affected = cmd.ExecuteNonQuery();
                    if (affected > 0)
                        return true;
                }

                const string insertSql = @"
INSERT INTO `column_settings`
(`settings_group`, `hidden_columns`, `column_order`, `column_width_settings`, `fixed_column_width_settings`,
 `row_height`, `font_size`, `table_height_enabled`, `table_height_mode`, `table_height`, `max_display_rows`,
 `left_columns`, `center_columns`, `right_columns`, `sort_specs`, `updated_at`)
VALUES
(@group, @hidden, @order, @width, @fixed, @rowHeight, @fontSize, @thEnabled, @thMode, @thHeight, @maxRows,
 @left, @center, @right, @sort, CURRENT_TIMESTAMP)";

                using (var cmd = new MySqlCommand(insertSql, conn))
                {
                    AddParameters(cmd, settings);
                    cmd.ExecuteNonQuery();
                }

                SaveToFallback(settings);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DataGridSettingsService] 保存到数据库失败: {ex.Message}");
                SaveToFallback(settings);
                return true;
            }
        }

        private static void AddParameters(MySqlCommand cmd, DataGridSettings settings)
        {
            cmd.Parameters.AddWithValue("@group", settings.SettingsGroup);
            cmd.Parameters.AddWithValue("@hidden", JsonSerializer.Serialize(settings.HiddenColumns));
            cmd.Parameters.AddWithValue("@order", JsonSerializer.Serialize(settings.ColumnOrder));
            cmd.Parameters.AddWithValue("@width", JsonSerializer.Serialize(settings.WidthSettings));
            cmd.Parameters.AddWithValue("@fixed", JsonSerializer.Serialize(settings.FixedWidths));
            cmd.Parameters.AddWithValue("@rowHeight", settings.RowHeight);
            cmd.Parameters.AddWithValue("@fontSize", settings.FontSize);
            cmd.Parameters.AddWithValue("@thEnabled", settings.TableHeightEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@thMode", settings.TableHeightMode ?? string.Empty);
            cmd.Parameters.AddWithValue("@thHeight", settings.TableHeight);
            cmd.Parameters.AddWithValue("@maxRows", settings.MaxDisplayRows);
            cmd.Parameters.AddWithValue("@left", JsonSerializer.Serialize(settings.LeftColumns));
            cmd.Parameters.AddWithValue("@center", JsonSerializer.Serialize(settings.CenterColumns));
            cmd.Parameters.AddWithValue("@right", JsonSerializer.Serialize(settings.RightColumns));
            cmd.Parameters.AddWithValue("@sort", JsonSerializer.Serialize(settings.SortSpecs.Select(s => $"{s.ColumnName}|{(s.Ascending ? "asc" : "desc")}")));
        }

        private static DataGridSettings? LoadFromFallback(string group)
        {
            try
            {
                var path = GetFallbackPath(group);
                if (!File.Exists(path)) return null;
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<DataGridSettings>(json);
            }
            catch
            {
                return null;
            }
        }

        private static void SaveToFallback(DataGridSettings settings)
        {
            try
            {
                Directory.CreateDirectory(FallbackDirectory);
                var path = GetFallbackPath(settings.SettingsGroup);
                File.WriteAllText(path, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
                // ignore
            }
        }

        private static string GetFallbackPath(string group)
        {
            var safe = string.Join("_", group.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(FallbackDirectory, $"{safe}.json");
        }

        private static List<string> ParseStringList(object value)
        {
            var text = value is DBNull || value == null ? null : value.ToString();
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(text);
                return list ?? new List<string>();
            }
            catch
            {
                return text.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => s.Trim())
                           .Where(s => !string.IsNullOrEmpty(s))
                           .ToList();
            }
        }

        private static Dictionary<string, string> ParseStringDict(object value)
        {
            var text = value is DBNull || value == null ? null : value.ToString();
            if (string.IsNullOrWhiteSpace(text)) return new Dictionary<string, string>();
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
                return dict ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private static Dictionary<string, int> ParseIntDict(object value)
        {
            var text = value is DBNull || value == null ? null : value.ToString();
            if (string.IsNullOrWhiteSpace(text)) return new Dictionary<string, int>();
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(text);
                return dict ?? new Dictionary<string, int>();
            }
            catch
            {
                return new Dictionary<string, int>();
            }
        }

        private static List<(string, bool)> ParseSortSpecs(object value)
        {
            var text = value is DBNull || value == null ? null : value.ToString();
            if (string.IsNullOrWhiteSpace(text)) return new List<(string, bool)>();
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(text) ?? new List<string>();
                var specs = new List<(string, bool)>();
                foreach (var item in list)
                {
                    if (string.IsNullOrWhiteSpace(item)) continue;
                    var parts = item.Split('|', 2);
                    var name = parts[0].Trim();
                    var asc = parts.Length < 2 || parts[1].Trim().ToLowerInvariant() != "desc";
                    specs.Add((name, asc));
                }
                return specs;
            }
            catch
            {
                return new List<(string, bool)>();
            }
        }
    }
}
