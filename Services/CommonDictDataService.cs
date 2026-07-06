using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text;
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

        public List<CommonDictDataRecord> ListByNodeCode(string nodeCode)
        {
            if (string.IsNullOrWhiteSpace(nodeCode))
            {
                return new List<CommonDictDataRecord>();
            }

            const string sql = @"SELECT id, node_code, code, name, field1, field2, field3, field4, field5,
number1, number2, date1, date2, flag1, flag2, amount, remark, sort_order, is_enabled
FROM common_dict_data
WHERE node_code=@node_code
ORDER BY sort_order ASC, id DESC";

            var result = new List<CommonDictDataRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@node_code", nodeCode.Trim());
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

        public void ReorderByCurrentView(List<CommonDictDataRecord> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            using var conn = CreateConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                const string sql = "UPDATE common_dict_data SET sort_order=@sort_order WHERE id=@id";
                using var cmd = new MySqlCommand(sql, conn, tx);
                cmd.Parameters.Add("@sort_order", MySqlDbType.Int32);
                cmd.Parameters.Add("@id", MySqlDbType.Int64);

                var sort = 1;
                foreach (var row in rows.Where(r => r.Id > 0))
                {
                    cmd.Parameters["@sort_order"].Value = sort++;
                    cmd.Parameters["@id"].Value = row.Id;
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public void UpdateMany(List<CommonDictDataRecord> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            using var conn = CreateConnection();
            using var tx = conn.BeginTransaction();
            try
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

                using var cmd = new MySqlCommand(sql, conn, tx);
                foreach (var row in rows.Where(x => x.Id > 0))
                {
                    cmd.Parameters.Clear();
                    FillParameters(cmd, row);
                    cmd.Parameters.AddWithValue("@id", row.Id);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public (int success, int fail) ImportFromCsv(string filePath, bool overwriteNode)
        {
            int success = 0;
            int fail = 0;

            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            if (lines.Length <= 1)
            {
                return (success, fail);
            }

            var header = CsvImportExportService.ParseCsvLine(lines[0]);
            var map = CsvImportExportService.BuildHeaderMap(header);
            var rows = new List<CommonDictDataRecord>();

            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                try
                {
                    var values = CsvImportExportService.ParseCsvLine(lines[i]);
                    var nodeCode = CsvImportExportService.GetMappedValue(values, map, "node_code", "节点编码").Trim();
                    var code = CsvImportExportService.GetMappedValue(values, map, "code", "编码").Trim();
                    var name = CsvImportExportService.GetMappedValue(values, map, "name", "名称").Trim();
                    if (string.IsNullOrWhiteSpace(nodeCode) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                    {
                        fail++;
                        continue;
                    }

                    rows.Add(new CommonDictDataRecord
                    {
                        NodeCode = nodeCode,
                        Code = code,
                        Name = name,
                        Field1 = CsvImportExportService.GetMappedValue(values, map, "field1", "字段1"),
                        Field2 = CsvImportExportService.GetMappedValue(values, map, "field2", "字段2"),
                        Field3 = CsvImportExportService.GetMappedValue(values, map, "field3", "字段3"),
                        Field4 = CsvImportExportService.GetMappedValue(values, map, "field4", "字段4"),
                        Field5 = CsvImportExportService.GetMappedValue(values, map, "field5", "字段5"),
                        Number1 = ParseNullableDecimal(CsvImportExportService.GetMappedValue(values, map, "number1", "数值1")),
                        Number2 = ParseNullableDecimal(CsvImportExportService.GetMappedValue(values, map, "number2", "数值2")),
                        Date1 = ParseNullableDate(CsvImportExportService.GetMappedValue(values, map, "date1", "日期1")),
                        Date2 = ParseNullableDate(CsvImportExportService.GetMappedValue(values, map, "date2", "日期2")),
                        Flag1 = ParseBool(CsvImportExportService.GetMappedValue(values, map, "flag1", "标记1")),
                        Flag2 = ParseBool(CsvImportExportService.GetMappedValue(values, map, "flag2", "标记2")),
                        Amount = ParseNullableDecimal(CsvImportExportService.GetMappedValue(values, map, "amount", "金额")),
                        Remark = CsvImportExportService.GetMappedValue(values, map, "remark", "备注"),
                        SortOrder = ParseIntOrDefault(CsvImportExportService.GetMappedValue(values, map, "sort_order", "排序"), 0),
                        IsEnabled = ParseBool(CsvImportExportService.GetMappedValue(values, map, "is_enabled", "启用", "启用状态"), true)
                    });
                }
                catch
                {
                    fail++;
                }
            }

            if (rows.Count == 0)
            {
                return (success, fail);
            }

            using var conn = CreateConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                if (overwriteNode)
                {
                    var nodes = rows.Select(x => x.NodeCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    foreach (var node in nodes)
                    {
                        using var clear = new MySqlCommand("DELETE FROM common_dict_data WHERE node_code=@node_code", conn, tx);
                        clear.Parameters.AddWithValue("@node_code", node);
                        clear.ExecuteNonQuery();
                    }
                }

                foreach (var row in rows)
                {
                    if (row.SortOrder <= 0)
                    {
                        row.SortOrder = success + 1;
                    }

                    const string findSql = "SELECT id FROM common_dict_data WHERE node_code=@node_code AND code=@code LIMIT 1";
                    using var find = new MySqlCommand(findSql, conn, tx);
                    find.Parameters.AddWithValue("@node_code", row.NodeCode);
                    find.Parameters.AddWithValue("@code", row.Code);
                    var idObj = find.ExecuteScalar();

                    if (idObj == null || idObj == DBNull.Value)
                    {
                        const string insertSql = @"INSERT INTO common_dict_data
(node_code, code, name, field1, field2, field3, field4, field5, number1, number2, date1, date2, flag1, flag2, amount, remark, sort_order, is_enabled)
VALUES
(@node_code, @code, @name, @field1, @field2, @field3, @field4, @field5, @number1, @number2, @date1, @date2, @flag1, @flag2, @amount, @remark, @sort_order, @is_enabled)";
                        using var insert = new MySqlCommand(insertSql, conn, tx);
                        FillParameters(insert, row);
                        insert.ExecuteNonQuery();
                    }
                    else
                    {
                        row.Id = Convert.ToInt64(idObj);
                        const string updateSql = @"UPDATE common_dict_data SET
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
                        using var update = new MySqlCommand(updateSql, conn, tx);
                        FillParameters(update, row);
                        update.Parameters.AddWithValue("@id", row.Id);
                        update.ExecuteNonQuery();
                    }

                    success++;
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }

            return (success, fail);
        }

        public void ExportToCsv(string filePath, IEnumerable<CommonDictDataRecord> rows)
        {
            CsvImportExportService.WriteCsv(
                filePath,
                new[] { "节点编码", "编码", "名称", "字段1", "字段2", "字段3", "字段4", "字段5", "数值1", "数值2", "日期1", "日期2", "标记1", "标记2", "金额", "备注", "排序", "启用" },
                rows.Select(r => new[]
                {
                    r.NodeCode,
                    r.Code,
                    r.Name,
                    r.Field1,
                    r.Field2,
                    r.Field3,
                    r.Field4,
                    r.Field5,
                    r.Number1?.ToString(CultureInfo.InvariantCulture),
                    r.Number2?.ToString(CultureInfo.InvariantCulture),
                    r.Date1?.ToString("yyyy-MM-dd"),
                    r.Date2?.ToString("yyyy-MM-dd"),
                    r.Flag1 ? "1" : "0",
                    r.Flag2 ? "1" : "0",
                    r.Amount?.ToString(CultureInfo.InvariantCulture),
                    r.Remark,
                    r.SortOrder.ToString(CultureInfo.InvariantCulture),
                    r.IsEnabled ? "1" : "0"
                }));
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

        private static int ParseIntOrDefault(string text, int fallback)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;
        }

        private static decimal? ParseNullableDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
            {
                return value;
            }

            return null;
        }

        private static DateTime? ParseNullableDate(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.StartsWith("0000-00-00", StringComparison.Ordinal))
            {
                return null;
            }

            if (DateTime.TryParseExact(text, new[] { "yyyy-MM-dd", "yyyy/MM/dd", "yyyyMMdd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                || DateTime.TryParse(text, out dt))
            {
                return dt.Date;
            }

            return null;
        }

        private static bool ParseBool(string text, bool fallback = false)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return fallback;
            }

            var value = text.Trim();
            return value == "1"
                   || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("是", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("启用", StringComparison.OrdinalIgnoreCase);
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
