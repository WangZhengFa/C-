using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 过度包装检测服务
    /// </summary>
    public class OverpackagingService
    {
        public List<OverpackagingRecord> ListAll()
        {
            const string sql = @"SELECT id, test_id, test_date, product_name, brand_series, shape_type, dimensions,
package_layers, package_weight, package_cost, sales_price, material, is_mixed, is_freeze_dried,
process_type, conclusion, remarks, inner_items_json
FROM overpacking_records
ORDER BY id DESC";

            var result = new List<OverpackagingRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new OverpackagingRecord
                {
                    Id = GetLong(reader, "id"),
                    TestId = GetString(reader, "test_id"),
                    TestDate = GetDate(reader, "test_date"),
                    ProductName = GetString(reader, "product_name"),
                    BrandSeries = GetString(reader, "brand_series"),
                    ShapeType = GetString(reader, "shape_type"),
                    Dimensions = GetString(reader, "dimensions"),
                    PackageLayers = GetInt(reader, "package_layers"),
                    PackageWeight = GetDecimal(reader, "package_weight"),
                    PackageCost = GetDecimal(reader, "package_cost"),
                    SalesPrice = GetDecimal(reader, "sales_price"),
                    Material = GetString(reader, "material"),
                    IsMixed = GetBool(reader, "is_mixed"),
                    IsFreezeDried = GetBool(reader, "is_freeze_dried"),
                    ProcessType = GetString(reader, "process_type"),
                    Conclusion = GetString(reader, "conclusion"),
                    Remarks = GetString(reader, "remarks"),
                    InnerItemsJson = GetString(reader, "inner_items_json")
                });
            }

            return result;
        }

        public long Insert(OverpackagingRecord record)
        {
            const string sql = @"INSERT INTO overpacking_records
(test_id, test_date, product_name, brand_series, shape_type, dimensions, package_layers,
 package_weight, package_cost, sales_price, material, is_mixed, is_freeze_dried, process_type,
 conclusion, remarks, inner_items_json)
VALUES
(@test_id, @test_date, @product_name, @brand_series, @shape_type, @dimensions, @package_layers,
 @package_weight, @package_cost, @sales_price, @material, @is_mixed, @is_freeze_dried, @process_type,
 @conclusion, @remarks, @inner_items_json);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(OverpackagingRecord record)
        {
            const string sql = @"UPDATE overpacking_records SET
test_id=@test_id,
test_date=@test_date,
product_name=@product_name,
brand_series=@brand_series,
shape_type=@shape_type,
dimensions=@dimensions,
package_layers=@package_layers,
package_weight=@package_weight,
package_cost=@package_cost,
sales_price=@sales_price,
material=@material,
is_mixed=@is_mixed,
is_freeze_dried=@is_freeze_dried,
process_type=@process_type,
conclusion=@conclusion,
remarks=@remarks,
inner_items_json=@inner_items_json
WHERE id=@id";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            cmd.Parameters.AddWithValue("@id", record.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(long id)
        {
            const string sql = "DELETE FROM overpacking_records WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, OverpackagingRecord record)
        {
            cmd.Parameters.AddWithValue("@test_id", (record.TestId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@test_date", ToDbDate(record.TestDate));
            cmd.Parameters.AddWithValue("@product_name", (record.ProductName ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@brand_series", (record.BrandSeries ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@shape_type", (record.ShapeType ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@dimensions", (record.Dimensions ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@package_layers", record.PackageLayers);
            cmd.Parameters.AddWithValue("@package_weight", record.PackageWeight);
            cmd.Parameters.AddWithValue("@package_cost", record.PackageCost);
            cmd.Parameters.AddWithValue("@sales_price", record.SalesPrice);
            cmd.Parameters.AddWithValue("@material", (record.Material ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@is_mixed", record.IsMixed ? 1 : 0);
            cmd.Parameters.AddWithValue("@is_freeze_dried", record.IsFreezeDried ? 1 : 0);
            cmd.Parameters.AddWithValue("@process_type", (record.ProcessType ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@conclusion", (record.Conclusion ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@remarks", (record.Remarks ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@inner_items_json", (record.InnerItemsJson ?? string.Empty).Trim());
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

        private static decimal GetDecimal(MySqlDataReader reader, string field)
        {
            var text = GetString(reader, field);
            return decimal.TryParse(text, out var value) ? value : 0m;
        }

        private static bool GetBool(MySqlDataReader reader, string field)
        {
            var text = GetString(reader, field);
            return text == "1" || bool.TryParse(text, out var v) && v;
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
