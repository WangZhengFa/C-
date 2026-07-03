using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 产品信息服务
    /// </summary>
    public class ProductInfoService
    {
        public List<ProductInfoRecord> ListAll()
        {
            const string sql = @"SELECT id, node_code, product_id, product_name, product_code, standard_code,
food_category, dosage_form, ownership_status, approval_method, approval_department, approval_date,
standard_validity, enterprise_code, enterprise_year, enterprise_effective_date, standard_link,
enterprise_link, remark, sort_order, is_enabled
FROM product_infos
ORDER BY sort_order ASC, id DESC";

            var result = new List<ProductInfoRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new ProductInfoRecord
                {
                    Id = GetLong(reader, "id"),
                    NodeCode = GetString(reader, "node_code"),
                    ProductId = GetString(reader, "product_id"),
                    ProductName = GetString(reader, "product_name"),
                    ProductCode = GetString(reader, "product_code"),
                    StandardCode = GetString(reader, "standard_code"),
                    FoodCategory = GetString(reader, "food_category"),
                    DosageForm = GetString(reader, "dosage_form"),
                    OwnershipStatus = GetString(reader, "ownership_status"),
                    ApprovalMethod = GetString(reader, "approval_method"),
                    ApprovalDepartment = GetString(reader, "approval_department"),
                    ApprovalDate = GetDate(reader, "approval_date"),
                    StandardValidity = GetString(reader, "standard_validity"),
                    EnterpriseCode = GetString(reader, "enterprise_code"),
                    EnterpriseYear = GetInt(reader, "enterprise_year"),
                    EnterpriseEffectiveDate = GetDate(reader, "enterprise_effective_date"),
                    StandardLink = GetString(reader, "standard_link"),
                    EnterpriseLink = GetString(reader, "enterprise_link"),
                    Remark = GetString(reader, "remark"),
                    SortOrder = GetInt(reader, "sort_order"),
                    IsEnabled = GetBool(reader, "is_enabled")
                });
            }

            return result;
        }

        public long Insert(ProductInfoRecord record)
        {
            const string sql = @"INSERT INTO product_infos
(node_code, product_id, product_name, product_code, standard_code, food_category, dosage_form,
 ownership_status, approval_method, approval_department, approval_date, standard_validity,
 enterprise_code, enterprise_year, enterprise_effective_date, standard_link, enterprise_link,
 remark, sort_order, is_enabled)
VALUES
(@node_code, @product_id, @product_name, @product_code, @standard_code, @food_category, @dosage_form,
 @ownership_status, @approval_method, @approval_department, @approval_date, @standard_validity,
 @enterprise_code, @enterprise_year, @enterprise_effective_date, @standard_link, @enterprise_link,
 @remark, @sort_order, @is_enabled);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(ProductInfoRecord record)
        {
            const string sql = @"UPDATE product_infos SET
node_code=@node_code,
product_id=@product_id,
product_name=@product_name,
product_code=@product_code,
standard_code=@standard_code,
food_category=@food_category,
dosage_form=@dosage_form,
ownership_status=@ownership_status,
approval_method=@approval_method,
approval_department=@approval_department,
approval_date=@approval_date,
standard_validity=@standard_validity,
enterprise_code=@enterprise_code,
enterprise_year=@enterprise_year,
enterprise_effective_date=@enterprise_effective_date,
standard_link=@standard_link,
enterprise_link=@enterprise_link,
remark=@remark,
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
            const string sql = "DELETE FROM product_infos WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, ProductInfoRecord record)
        {
            cmd.Parameters.AddWithValue("@node_code", (record.NodeCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@product_id", (record.ProductId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@product_name", (record.ProductName ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@product_code", (record.ProductCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@standard_code", (record.StandardCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@food_category", (record.FoodCategory ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@dosage_form", (record.DosageForm ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@ownership_status", (record.OwnershipStatus ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@approval_method", (record.ApprovalMethod ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@approval_department", (record.ApprovalDepartment ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@approval_date", ToDbDate(record.ApprovalDate));
            cmd.Parameters.AddWithValue("@standard_validity", (record.StandardValidity ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@enterprise_code", (record.EnterpriseCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@enterprise_year", record.EnterpriseYear);
            cmd.Parameters.AddWithValue("@enterprise_effective_date", ToDbDate(record.EnterpriseEffectiveDate));
            cmd.Parameters.AddWithValue("@standard_link", (record.StandardLink ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@enterprise_link", (record.EnterpriseLink ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@remark", (record.Remark ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sort_order", record.SortOrder);
            cmd.Parameters.AddWithValue("@is_enabled", record.IsEnabled ? 1 : 0);
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
