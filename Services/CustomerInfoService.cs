using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 客户信息服务
    /// </summary>
    public class CustomerInfoService
    {
        public List<CustomerInfoRecord> ListAll()
        {
            const string sql = @"SELECT id, customer_id, source, customer_name, customer_type, license_no,
license_validity, business_license, business_validity, contact_address, postal_code,
contact_person, contact_phone, is_disabled, remark
FROM customer_infos
ORDER BY id DESC";

            var result = new List<CustomerInfoRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new CustomerInfoRecord
                {
                    Id = GetLong(reader, "id"),
                    CustomerId = GetString(reader, "customer_id"),
                    Source = GetString(reader, "source"),
                    CustomerName = GetString(reader, "customer_name"),
                    CustomerType = GetString(reader, "customer_type"),
                    LicenseNo = GetString(reader, "license_no"),
                    LicenseValidity = GetString(reader, "license_validity"),
                    BusinessLicense = GetString(reader, "business_license"),
                    BusinessValidity = GetString(reader, "business_validity"),
                    ContactAddress = GetString(reader, "contact_address"),
                    PostalCode = GetString(reader, "postal_code"),
                    ContactPerson = GetString(reader, "contact_person"),
                    ContactPhone = GetString(reader, "contact_phone"),
                    IsDisabled = GetBool(reader, "is_disabled"),
                    Remark = GetString(reader, "remark")
                });
            }

            return result;
        }

        public long Insert(CustomerInfoRecord record)
        {
            const string sql = @"INSERT INTO customer_infos
(customer_id, source, customer_name, customer_type, license_no, license_validity, business_license,
 business_validity, contact_address, postal_code, contact_person, contact_phone, is_disabled, remark)
VALUES
(@customer_id, @source, @customer_name, @customer_type, @license_no, @license_validity, @business_license,
 @business_validity, @contact_address, @postal_code, @contact_person, @contact_phone, @is_disabled, @remark);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(CustomerInfoRecord record)
        {
            const string sql = @"UPDATE customer_infos SET
customer_id=@customer_id,
source=@source,
customer_name=@customer_name,
customer_type=@customer_type,
license_no=@license_no,
license_validity=@license_validity,
business_license=@business_license,
business_validity=@business_validity,
contact_address=@contact_address,
postal_code=@postal_code,
contact_person=@contact_person,
contact_phone=@contact_phone,
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
            const string sql = "DELETE FROM customer_infos WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, CustomerInfoRecord record)
        {
            cmd.Parameters.AddWithValue("@customer_id", (record.CustomerId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@source", (record.Source ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@customer_name", (record.CustomerName ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@customer_type", (record.CustomerType ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@license_no", (record.LicenseNo ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@license_validity", (record.LicenseValidity ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@business_license", (record.BusinessLicense ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@business_validity", (record.BusinessValidity ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@contact_address", (record.ContactAddress ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@postal_code", (record.PostalCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@contact_person", (record.ContactPerson ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@contact_phone", (record.ContactPhone ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@is_disabled", record.IsDisabled ? 1 : 0);
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
