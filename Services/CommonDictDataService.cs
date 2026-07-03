using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 通用字典数据服务
    /// </summary>
    public class CommonDictDataService
    {
        public List<CommonDictDataRecord> ListAll()
        {
            const string sql = @"SELECT id, node_code, code, name, field1, field2, field3, field4, field5,
number1, number2, date1, date2, flag1, flag2, amount, remark, sort_order, is_enabled
FROM common_dict_data
ORDER BY sort_order ASC, id DESC";

            var result = new List<CommonDictDataRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new CommonDictDataRecord
                {
                    Id = GetLong(reader, "id"),
                    NodeCode = GetString(reader, "node_code"),
                    Code = GetString(reader, "code"),
                    Name = GetString(reader, "name"),
                    Field1 = GetString(reader, "field1"),
                    Field2 = GetString(reader, "field2"),
                    Field3 = GetString(reader, "field3"),
                    Field4 = GetString(reader, "field4"),
                    Field5 = GetString(reader, "field5"),
                    Number1 = GetDecimal(reader, "number1"),
                    Number2 = GetDecimal(reader, "number2"),
                    Date1 = GetDate(reader, "date1"),
                    Date2 = GetDate(reader, "date2"),
                    Flag1 = GetBool(reader, "flag1"),
                    Flag2 = GetBool(reader, "flag2"),
                    Amount = GetDecimal(reader, "amount"),
                    Remark = GetString(reader, "remark"),
                    SortOrder = GetInt(reader, "sort_order"),
                    IsEnabled = GetBool(reader, "is_enabled")
                });
            }

            return result;
        }

        public long Insert(CommonDictDataRecord record)
        {
            const string sql = @"INSERT INTO common_dict_data
(node_code, code, name, field1, field2, field3, field4, field5, number1, number2, date1, date2, flag1, flag2, amount, remark, sort_order, is_enabled)
VALUES
(@node_code, @code, @name, @field1, @field2, @field3, @field4, @field5, @number1, @number2, @date1, @date2, @flag1, @flag2, @amount, @remark, @sort_order, @is_enabled);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(CommonDictDataRecord record)
        {
            const string sql = @"UPDATE common_dict_data SET
node_code=@node_code,
code=@code,
name=@name,
field1=@field1,
field2=@field2,
field3=@field3,
field4=@field4,
field5=@field5,
number1=@number1,
number2=@number2,
date1=@date1,
date2=@date2,
flag1=@flag1,
flag2=@flag2,
amount=@amount,
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
            const string sql = "DELETE FROM common_dict_data WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, CommonDictDataRecord record)
        {
            cmd.Parameters.AddWithValue("@node_code", (record.NodeCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@code", (record.Code ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@name", (record.Name ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@field1", (record.Field1 ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@field2", (record.Field2 ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@field3", (record.Field3 ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@field4", (record.Field4 ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@field5", (record.Field5 ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@number1", ToDbNumber(record.Number1));
            cmd.Parameters.AddWithValue("@number2", ToDbNumber(record.Number2));
            cmd.Parameters.AddWithValue("@date1", ToDbDate(record.Date1));
            cmd.Parameters.AddWithValue("@date2", ToDbDate(record.Date2));
            cmd.Parameters.AddWithValue("@flag1", record.Flag1 ? 1 : 0);
            cmd.Parameters.AddWithValue("@flag2", record.Flag2 ? 1 : 0);
            cmd.Parameters.AddWithValue("@amount", ToDbNumber(record.Amount));
            cmd.Parameters.AddWithValue("@remark", (record.Remark ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@sort_order", record.SortOrder);
            cmd.Parameters.AddWithValue("@is_enabled", record.IsEnabled ? 1 : 0);
        }

        private static object ToDbNumber(decimal? value)
        {
            return value.HasValue ? value.Value : DBNull.Value;
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
