using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 员工信息实体
    /// </summary>
    public class EmployeeInfo
    {
        public long Id { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime? HireDate { get; set; } = DateTime.Today;
        public string IdCardNo { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string GraduationSchool { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = "在职";
        public string Remark { get; set; } = string.Empty;
    }
}