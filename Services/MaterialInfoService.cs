using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 物料信息服务
    /// </summary>
    public class MaterialInfoService
    {
        public List<MaterialInfoRecord> ListAll()
        {
            const string sql = @"SELECT id, material_id, node_code, first_level_code, material_code, material_name,
specification, packaging_spec, brand_series, expiry_date, unit, standard, is_disabled, remark
FROM material_infos
ORDER BY id DESC";

            var result = new List<MaterialInfoRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new MaterialInfoRecord
                {
                    Id = GetLong(reader, "id"),
                    MaterialId = GetString(reader, "material_id"),
                    NodeCode = GetString(reader, "node_code"),
                    FirstLevelCode = GetString(reader, "first_level_code"),
                    MaterialCode = GetString(reader, "material_code"),
                    MaterialName = GetString(reader, "material_name"),
                    Specification = GetString(reader, "specification"),
                    PackagingSpec = GetString(reader, "packaging_spec"),
                    BrandSeries = GetString(reader, "brand_series"),
                    ExpiryDate = GetInt(reader, "expiry_date"),
                    Unit = GetString(reader, "unit"),
                    Standard = GetString(reader, "standard"),
                    IsDisabled = GetBool(reader, "is_disabled"),
                    Remark = GetString(reader, "remark")
                });
            }

            return result;
        }

        public long Insert(MaterialInfoRecord record)
        {
            const string sql = @"INSERT INTO material_infos
(material_id, node_code, first_level_code, material_code, material_name, specification,
 packaging_spec, brand_series, expiry_date, unit, standard, is_disabled, remark)
VALUES
(@material_id, @node_code, @first_level_code, @material_code, @material_name, @specification,
 @packaging_spec, @brand_series, @expiry_date, @unit, @standard, @is_disabled, @remark);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(MaterialInfoRecord record)
        {
            const string sql = @"UPDATE material_infos SET
material_id=@material_id,
node_code=@node_code,
first_level_code=@first_level_code,
material_code=@material_code,
material_name=@material_name,
specification=@specification,
packaging_spec=@packaging_spec,
brand_series=@brand_series,
expiry_date=@expiry_date,
unit=@unit,
standard=@standard,
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
            const string sql = "DELETE FROM material_infos WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, MaterialInfoRecord record)
        {
            cmd.Parameters.AddWithValue("@material_id", (record.MaterialId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@node_code", (record.NodeCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@first_level_code", (record.FirstLevelCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@material_code", (record.MaterialCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@material_name", (record.MaterialName ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@specification", (record.Specification ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@packaging_spec", (record.PackagingSpec ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@brand_series", (record.BrandSeries ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@expiry_date", record.ExpiryDate);
            cmd.Parameters.AddWithValue("@unit", (record.Unit ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@standard", (record.Standard ?? string.Empty).Trim());
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
