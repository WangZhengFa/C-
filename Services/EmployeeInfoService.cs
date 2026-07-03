using System;
using System.Collections.Generic;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Models;
using MySqlConnector;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 员工信息业务服务
    /// </summary>
    public class EmployeeInfoService
    {
        public List<EmployeeInfo> ListAll()
        {
            const string sql = @"SELECT id, employee_id, employee_name, department, title, position, hire_date,
id_card_no, phone, gender, graduation_school, education, email, status, remark
FROM employee_infos
ORDER BY id DESC";

            var result = new List<EmployeeInfo>();
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new EmployeeInfo
                {
                    Id = GetLong(reader, "id"),
                    EmployeeId = GetString(reader, "employee_id"),
                    EmployeeName = GetString(reader, "employee_name"),
                    Department = GetString(reader, "department"),
                    Title = GetString(reader, "title"),
                    Position = GetString(reader, "position"),
                    HireDate = GetDate(reader, "hire_date"),
                    IdCardNo = GetString(reader, "id_card_no"),
                    Phone = GetString(reader, "phone"),
                    Gender = GetString(reader, "gender"),
                    GraduationSchool = GetString(reader, "graduation_school"),
                    Education = GetString(reader, "education"),
                    Email = GetString(reader, "email"),
                    Status = GetString(reader, "status"),
                    Remark = GetString(reader, "remark")
                });
            }

            return result;
        }

        public EmployeeInfo? GetById(long id)
        {
            const string sql = @"SELECT id, employee_id, employee_name, department, title, position, hire_date,
id_card_no, phone, gender, graduation_school, education, email, status, remark
FROM employee_infos WHERE id = @id LIMIT 1";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new EmployeeInfo
            {
                Id = GetLong(reader, "id"),
                EmployeeId = GetString(reader, "employee_id"),
                EmployeeName = GetString(reader, "employee_name"),
                Department = GetString(reader, "department"),
                Title = GetString(reader, "title"),
                Position = GetString(reader, "position"),
                HireDate = GetDate(reader, "hire_date"),
                IdCardNo = GetString(reader, "id_card_no"),
                Phone = GetString(reader, "phone"),
                Gender = GetString(reader, "gender"),
                GraduationSchool = GetString(reader, "graduation_school"),
                Education = GetString(reader, "education"),
                Email = GetString(reader, "email"),
                Status = GetString(reader, "status"),
                Remark = GetString(reader, "remark")
            };
        }

        public long Insert(EmployeeInfo record)
        {
            const string sql = @"INSERT INTO employee_infos
(employee_id, employee_name, department, title, position, hire_date, id_card_no, phone, gender,
 graduation_school, education, email, status, remark)
VALUES
(@employee_id, @employee_name, @department, @title, @position, @hire_date, @id_card_no, @phone, @gender,
 @graduation_school, @education, @email, @status, @remark);
SELECT LAST_INSERT_ID();";

            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            FillParameters(cmd, record);
            var obj = cmd.ExecuteScalar();
            return obj == null ? 0 : Convert.ToInt64(obj);
        }

        public void Update(EmployeeInfo record)
        {
            const string sql = @"UPDATE employee_infos SET
employee_id=@employee_id,
employee_name=@employee_name,
department=@department,
title=@title,
position=@position,
hire_date=@hire_date,
id_card_no=@id_card_no,
phone=@phone,
gender=@gender,
graduation_school=@graduation_school,
education=@education,
email=@email,
status=@status,
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
            const string sql = "DELETE FROM employee_infos WHERE id=@id";
            using var conn = CreateConnection();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void FillParameters(MySqlCommand cmd, EmployeeInfo record)
        {
            cmd.Parameters.AddWithValue("@employee_id", (record.EmployeeId ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@employee_name", (record.EmployeeName ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@department", (record.Department ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@title", (record.Title ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@position", (record.Position ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@hire_date", record.HireDate?.Date ?? DateTime.Today);
            cmd.Parameters.AddWithValue("@id_card_no", (record.IdCardNo ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@phone", (record.Phone ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@gender", (record.Gender ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@graduation_school", (record.GraduationSchool ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@education", (record.Education ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@email", (record.Email ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(record.Status) ? "在职" : record.Status.Trim());
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