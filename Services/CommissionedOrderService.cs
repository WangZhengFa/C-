using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 委托订单服务
    /// </summary>
    public class CommissionedOrderService
    {
        public List<CommissionedOrderRecord> ListAll()
        {
            const string sql = @"SELECT id, order_id, order_date, customer_name, contact_person, contact_phone,
product_name, product_spec, order_quantity, order_type, inspection_items, inspection_standard,
required_date, actual_date, order_status, inspection_fee, payment_status, remark
FROM order_forms
ORDER BY id DESC";

            var result = new List<CommissionedOrderRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new CommissionedOrderRecord
                {
                    Id = GetLong(reader, "id"),
                    OrderId = GetString(reader, "order_id"),
                    OrderDate = GetDate(reader, "order_date"),
                    CustomerName = GetString(reader, "customer_name"),
                    ContactPerson = GetString(reader, "contact_person"),
                    ContactPhone = GetString(reader, "contact_phone"),
                    ProductName = GetString(reader, "product_name"),
                    ProductSpec = GetString(reader, "product_spec"),
                    OrderQuantity = GetString(reader, "order_quantity"),
                    OrderType = GetString(reader, "order_type"),
                    InspectionItems = GetString(reader, "inspection_items"),
                    InspectionStandard = GetString(reader, "inspection_standard"),
                    RequiredDate = GetDate(reader, "required_date"),
                    ActualDate = GetDate(reader, "actual_date"),
                    OrderStatus = GetString(reader, "order_status"),
                    InspectionFee = GetString(reader, "inspection_fee"),
                    PaymentStatus = GetString(reader, "payment_status"),
                    Remark = GetString(reader, "remark")
                });
            }

            return result;
        }

        public long Insert(CommissionedOrderRecord record)
        {
            const string sql = @"INSERT INTO order_forms
(order_id, order_date, customer_name, contact_person, contact_phone, product_name, product_spec,
 order_quantity, order_type, inspection_items, inspection_standard, required_date, actual_date,
 order_status, inspection_fee, payment_status, remark)
VALUES
(@order_id, @order_date, @customer_name, @contact_person, @contact_phone, @product_name, @product_spec,
 @order_quantity, @order_type, @inspection_items, @inspection_standard, @required_date, @actual_date,
 @order_status, @inspection_fee, @payment_status, @remark);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(CommissionedOrderRecord record)
        {
            const string sql = @"UPDATE order_forms SET
order_id=@order_id,
order_date=@order_date,
customer_name=@customer_name,
contact_person=@contact_person,
contact_phone=@contact_phone,
product_name=@product_name,
product_spec=@product_spec,
order_quantity=@order_quantity,
order_type=@order_type,
inspection_items=@inspection_items,
inspection_standard=@inspection_standard,
required_date=@required_date,
actual_date=@actual_date,
order_status=@order_status,
inspection_fee=@inspection_fee,
payment_status=@payment_status,
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
            const string sql = "DELETE FROM order_forms WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, CommissionedOrderRecord record)
        {
            cmd.Parameters.AddWithValue("@order_id", (record.OrderId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@order_date", ToDbDate(record.OrderDate));
            cmd.Parameters.AddWithValue("@customer_name", (record.CustomerName ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@contact_person", (record.ContactPerson ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@contact_phone", (record.ContactPhone ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@product_name", (record.ProductName ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@product_spec", (record.ProductSpec ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@order_quantity", (record.OrderQuantity ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@order_type", (record.OrderType ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@inspection_items", (record.InspectionItems ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@inspection_standard", (record.InspectionStandard ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@required_date", ToDbDate(record.RequiredDate));
            cmd.Parameters.AddWithValue("@actual_date", ToDbDate(record.ActualDate));
            cmd.Parameters.AddWithValue("@order_status", (record.OrderStatus ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@inspection_fee", (record.InspectionFee ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@payment_status", (record.PaymentStatus ?? string.Empty).Trim());
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
