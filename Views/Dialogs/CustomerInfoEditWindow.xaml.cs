using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 客户信息编辑窗口
    /// </summary>
    public partial class CustomerInfoEditWindow : Window
    {
        public CustomerInfoRecord Value { get; private set; }

        public CustomerInfoEditWindow(CustomerInfoRecord? source, IEnumerable<CustomerInfoRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new CustomerInfoRecord() : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<CustomerInfoRecord> existing)
        {
            SourceCombo.Items.Clear();
            SourceCombo.Items.Add("客户录入");
            SourceCombo.Items.Add("导入");
            SourceCombo.Items.Add("系统同步");

            CustomerTypeCombo.Items.Clear();
            CustomerTypeCombo.Items.Add("生产企业");
            CustomerTypeCombo.Items.Add("经营企业");
            CustomerTypeCombo.Items.Add("个体工商户");

            foreach (var value in existing.Select(x => x.Source).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!SourceCombo.Items.Contains(value))
                {
                    SourceCombo.Items.Add(value);
                }
            }

            foreach (var value in existing.Select(x => x.CustomerType).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!CustomerTypeCombo.Items.Contains(value))
                {
                    CustomerTypeCombo.Items.Add(value);
                }
            }
        }

        private void BindValue()
        {
            CustomerIdText.Text = Value.CustomerId;
            SourceCombo.Text = Value.Source;
            CustomerNameText.Text = Value.CustomerName;
            CustomerTypeCombo.Text = Value.CustomerType;
            LicenseNoText.Text = Value.LicenseNo;
            LicenseValidityText.Text = Value.LicenseValidity;
            BusinessLicenseText.Text = Value.BusinessLicense;
            BusinessValidityText.Text = Value.BusinessValidity;
            ContactAddressText.Text = Value.ContactAddress;
            PostalCodeText.Text = Value.PostalCode;
            ContactPersonText.Text = Value.ContactPerson;
            ContactPhoneText.Text = Value.ContactPhone;
            IsDisabledCheck.IsChecked = Value.IsDisabled;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var customerName = CustomerNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(customerName))
            {
                MessageBox.Show(this, "客户名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.CustomerId = CustomerIdText.Text.Trim();
            Value.Source = SourceCombo.Text.Trim();
            Value.CustomerName = customerName;
            Value.CustomerType = CustomerTypeCombo.Text.Trim();
            Value.LicenseNo = LicenseNoText.Text.Trim();
            Value.LicenseValidity = LicenseValidityText.Text.Trim();
            Value.BusinessLicense = BusinessLicenseText.Text.Trim();
            Value.BusinessValidity = BusinessValidityText.Text.Trim();
            Value.ContactAddress = ContactAddressText.Text.Trim();
            Value.PostalCode = PostalCodeText.Text.Trim();
            Value.ContactPerson = ContactPersonText.Text.Trim();
            Value.ContactPhone = ContactPhoneText.Text.Trim();
            Value.IsDisabled = IsDisabledCheck.IsChecked == true;
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static CustomerInfoRecord Clone(CustomerInfoRecord source)
        {
            return new CustomerInfoRecord
            {
                Id = source.Id,
                CustomerId = source.CustomerId,
                Source = source.Source,
                CustomerName = source.CustomerName,
                CustomerType = source.CustomerType,
                LicenseNo = source.LicenseNo,
                LicenseValidity = source.LicenseValidity,
                BusinessLicense = source.BusinessLicense,
                BusinessValidity = source.BusinessValidity,
                ContactAddress = source.ContactAddress,
                PostalCode = source.PostalCode,
                ContactPerson = source.ContactPerson,
                ContactPhone = source.ContactPhone,
                IsDisabled = source.IsDisabled,
                Remark = source.Remark
            };
        }
    }
}
