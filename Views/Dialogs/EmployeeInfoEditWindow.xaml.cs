using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 员工信息编辑窗口
    /// </summary>
    public partial class EmployeeInfoEditWindow : Window
    {
        public EmployeeInfo Value { get; private set; }

        public EmployeeInfoEditWindow(EmployeeInfo? source, IEnumerable<EmployeeInfo> existing)
        {
            InitializeComponent();
            Value = source == null ? new EmployeeInfo { HireDate = DateTime.Today, Status = "在职" } : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<EmployeeInfo> existing)
        {
            var all = existing?.ToList() ?? new List<EmployeeInfo>();

            FillEditableCombo(DepartmentCombo, all.Select(x => x.Department), new[] { "生产部", "质检部", "研发部", "行政部" });
            FillEditableCombo(TitleCombo, all.Select(x => x.Title), new[] { "技术员", "工程师", "主管", "经理" });
            FillEditableCombo(PositionCombo, all.Select(x => x.Position), new[] { "检验岗", "取样岗", "化验岗", "管理岗" });
            FillEditableCombo(EducationCombo, all.Select(x => x.Education), new[] { "高中", "大专", "本科", "硕士", "博士" });

            GenderCombo.Items.Clear();
            GenderCombo.Items.Add("男");
            GenderCombo.Items.Add("女");

            StatusCombo.Items.Clear();
            StatusCombo.Items.Add("在职");
            StatusCombo.Items.Add("离职");
            StatusCombo.Items.Add("停职");
        }

        private static void FillEditableCombo(System.Windows.Controls.ComboBox combo, IEnumerable<string> fromData, IEnumerable<string> presets)
        {
            combo.Items.Clear();
            foreach (var item in presets.Concat(fromData)
                                        .Where(x => !string.IsNullOrWhiteSpace(x))
                                        .Select(x => x.Trim())
                                        .Distinct()
                                        .OrderBy(x => x))
            {
                combo.Items.Add(item);
            }
        }

        private void BindValue()
        {
            EmployeeIdText.Text = Value.EmployeeId;
            EmployeeNameText.Text = Value.EmployeeName;
            DepartmentCombo.Text = Value.Department;
            TitleCombo.Text = Value.Title;
            PositionCombo.Text = Value.Position;
            HireDatePicker.SelectedDate = Value.HireDate;
            IdCardNoText.Text = Value.IdCardNo;
            PhoneText.Text = Value.Phone;
            GenderCombo.Text = Value.Gender;
            GraduationSchoolText.Text = Value.GraduationSchool;
            EducationCombo.Text = Value.Education;
            EmailText.Text = Value.Email;
            StatusCombo.Text = string.IsNullOrWhiteSpace(Value.Status) ? "在职" : Value.Status;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var employeeId = EmployeeIdText.Text.Trim();
            var employeeName = EmployeeNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                MessageBox.Show(this, "工号不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(employeeName))
            {
                MessageBox.Show(this, "姓名不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var email = EmailText.Text.Trim();
            if (!string.IsNullOrWhiteSpace(email) && !email.Contains("@"))
            {
                MessageBox.Show(this, "邮箱格式不正确", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.EmployeeId = employeeId;
            Value.EmployeeName = employeeName;
            Value.Department = DepartmentCombo.Text.Trim();
            Value.Title = TitleCombo.Text.Trim();
            Value.Position = PositionCombo.Text.Trim();
            Value.HireDate = HireDatePicker.SelectedDate;
            Value.IdCardNo = IdCardNoText.Text.Trim();
            Value.Phone = PhoneText.Text.Trim();
            Value.Gender = GenderCombo.Text.Trim();
            Value.GraduationSchool = GraduationSchoolText.Text.Trim();
            Value.Education = EducationCombo.Text.Trim();
            Value.Email = email;
            Value.Status = string.IsNullOrWhiteSpace(StatusCombo.Text) ? "在职" : StatusCombo.Text.Trim();
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static EmployeeInfo Clone(EmployeeInfo source)
        {
            return new EmployeeInfo
            {
                Id = source.Id,
                EmployeeId = source.EmployeeId,
                EmployeeName = source.EmployeeName,
                Department = source.Department,
                Title = source.Title,
                Position = source.Position,
                HireDate = source.HireDate,
                IdCardNo = source.IdCardNo,
                Phone = source.Phone,
                Gender = source.Gender,
                GraduationSchool = source.GraduationSchool,
                Education = source.Education,
                Email = source.Email,
                Status = source.Status,
                Remark = source.Remark
            };
        }
    }
}