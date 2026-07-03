using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 检品参数服务
    /// </summary>
    public class InspectionParamsService
    {
        public List<InspectionParamsRecord> ListAll()
        {
            const string sql = @"SELECT id, inspection_id, node_code, inspection_name, material_code,
standard, specification, is_disabled, remark
FROM inspection_params
ORDER BY id DESC";

            var result = new List<InspectionParamsRecord>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new InspectionParamsRecord
                {
                    Id = GetLong(reader, "id"),
                    InspectionId = GetString(reader, "inspection_id"),
                    NodeCode = GetString(reader, "node_code"),
                    InspectionName = GetString(reader, "inspection_name"),
                    MaterialCode = GetString(reader, "material_code"),
                    Standard = GetString(reader, "standard"),
                    Specification = GetString(reader, "specification"),
                    IsDisabled = GetBool(reader, "is_disabled"),
                    Remark = GetString(reader, "remark")
                });
            }

            return result;
        }

        public long Insert(InspectionParamsRecord record)
        {
            const string sql = @"INSERT INTO inspection_params
(inspection_id, node_code, inspection_name, material_code, standard, specification, is_disabled, remark)
VALUES
(@inspection_id, @node_code, @inspection_name, @material_code, @standard, @specification, @is_disabled, @remark);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(InspectionParamsRecord record)
        {
            const string sql = @"UPDATE inspection_params SET
inspection_id=@inspection_id,
node_code=@node_code,
inspection_name=@inspection_name,
material_code=@material_code,
standard=@standard,
specification=@specification,
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
            const string sql = "DELETE FROM inspection_params WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, InspectionParamsRecord record)
        {
            cmd.Parameters.AddWithValue("@inspection_id", (record.InspectionId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@node_code", (record.NodeCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@inspection_name", (record.InspectionName ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@material_code", (record.MaterialCode ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@standard", (record.Standard ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@specification", (record.Specification ?? string.Empty).Trim());
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
