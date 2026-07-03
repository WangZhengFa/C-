using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 产品条码服务
    /// </summary>
    public class ProductBarcodeService
    {
        public List<ProductBarcodeRecord> ListAll()
        {
            const string sql = @"SELECT id, barcode_id, company_code, barcode_number, product_id, product_name,
brand_series, package_category, package_spec, net_content, unit, generate_date, is_disabled, remark
FROM product_barcodes
ORDER BY id DESC";

            var result = new List<ProductBarcodeRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new ProductBarcodeRecord
                {
                    Id = GetLong(reader, "id"),
                    BarcodeId = GetString(reader, "barcode_id"),
                    CompanyCode = GetString(reader, "company_code"),
                    BarcodeNumber = GetString(reader, "barcode_number"),
                    ProductId = GetString(reader, "product_id"),
                    ProductName = GetString(reader, "product_name"),
                    BrandSeries = GetString(reader, "brand_series"),
                    PackageCategory = GetString(reader, "package_category"),
                    PackageSpec = GetString(reader, "package_spec"),
                    NetContent = GetString(reader, "net_content"),
                    Unit = GetString(reader, "unit"),
                    GenerateDate = GetDate(reader, "generate_date"),
                    IsDisabled = GetBool(reader, "is_disabled"),
                    Remark = GetString(reader, "remark")
                });
            }

            return result;
        }

        public long Insert(ProductBarcodeRecord record)
        {
            const string sql = @"INSERT INTO product_barcodes
(barcode_id, company_code, barcode_number, product_id, product_name, brand_series,
 package_category, package_spec, net_content, unit, generate_date, is_disabled, remark)
VALUES
(@barcode_id, @company_code, @barcode_number, @product_id, @product_name, @brand_series,
 @package_category, @package_spec, @net_content, @unit, @generate_date, @is_disabled, @remark);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(ProductBarcodeRecord record)
        {
            const string sql = @"UPDATE product_barcodes SET
barcode_id=@barcode_id,
company_code=@company_code,
barcode_number=@barcode_number,
product_id=@product_id,
product_name=@product_name,
brand_series=@brand_series,
package_category=@package_category,
package_spec=@package_spec,
net_content=@net_content,
unit=@unit,
generate_date=@generate_date,
is_disabled=@is_disabled,
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
            const string sql = "DELETE FROM product_barcodes WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, ProductBarcodeRecord record)
        {
            cmd.Parameters.AddWithValue("@barcode_id", (record.BarcodeId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@company_code", (record.CompanyCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@barcode_number", (record.BarcodeNumber ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@product_id", (record.ProductId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@product_name", (record.ProductName ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@brand_series", (record.BrandSeries ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@package_category", (record.PackageCategory ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@package_spec", (record.PackageSpec ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@net_content", (record.NetContent ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@unit", (record.Unit ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@generate_date", ToDbDate(record.GenerateDate));
            cmd.Parameters.AddWithValue("@is_disabled", record.IsDisabled ? 1 : 0);
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
    }
}
