using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 委托订单编辑窗口
    /// </summary>
    public partial class CommissionedOrderEditWindow : Window
    {
        public CommissionedOrderRecord Value { get; private set; }

        public CommissionedOrderEditWindow(CommissionedOrderRecord? source, IEnumerable<CommissionedOrderRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new CommissionedOrderRecord() : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<CommissionedOrderRecord> existing)
        {
            OrderTypeCombo.Items.Clear();
            OrderTypeCombo.Items.Add("常规委托");
            OrderTypeCombo.Items.Add("加急委托");

            OrderStatusCombo.Items.Clear();
            OrderStatusCombo.Items.Add("待处理");
            OrderStatusCombo.Items.Add("处理中");
            OrderStatusCombo.Items.Add("已完成");
            OrderStatusCombo.Items.Add("已取消");

            PaymentStatusCombo.Items.Clear();
            PaymentStatusCombo.Items.Add("未付款");
            PaymentStatusCombo.Items.Add("部分付款");
            PaymentStatusCombo.Items.Add("已付款");

            foreach (var value in existing.Select(x => x.OrderType).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!OrderTypeCombo.Items.Contains(value))
                {
                    OrderTypeCombo.Items.Add(value);
                }
            }

            foreach (var value in existing.Select(x => x.OrderStatus).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!OrderStatusCombo.Items.Contains(value))
                {
                    OrderStatusCombo.Items.Add(value);
                }
            }

            foreach (var value in existing.Select(x => x.PaymentStatus).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!PaymentStatusCombo.Items.Contains(value))
                {
                    PaymentStatusCombo.Items.Add(value);
                }
            }
        }

        private void BindValue()
        {
            OrderIdText.Text = Value.OrderId;
            OrderDatePicker.SelectedDate = Value.OrderDate;
            CustomerNameText.Text = Value.CustomerName;
            ContactPersonText.Text = Value.ContactPerson;
            ContactPhoneText.Text = Value.ContactPhone;
            ProductNameText.Text = Value.ProductName;
            ProductSpecText.Text = Value.ProductSpec;
            OrderQuantityText.Text = Value.OrderQuantity;
            OrderTypeCombo.Text = Value.OrderType;
            InspectionItemsText.Text = Value.InspectionItems;
            InspectionStandardText.Text = Value.InspectionStandard;
            RequiredDatePicker.SelectedDate = Value.RequiredDate;
            ActualDatePicker.SelectedDate = Value.ActualDate;
            OrderStatusCombo.Text = Value.OrderStatus;
            InspectionFeeText.Text = Value.InspectionFee;
            PaymentStatusCombo.Text = Value.PaymentStatus;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var orderId = OrderIdText.Text.Trim();
            var customerName = CustomerNameText.Text.Trim();
            var productName = ProductNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(orderId))
            {
                MessageBox.Show(this, "订单编号不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(customerName))
            {
                MessageBox.Show(this, "客户名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(productName))
            {
                MessageBox.Show(this, "产品名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.OrderId = orderId;
            Value.OrderDate = OrderDatePicker.SelectedDate;
            Value.CustomerName = customerName;
            Value.ContactPerson = ContactPersonText.Text.Trim();
            Value.ContactPhone = ContactPhoneText.Text.Trim();
            Value.ProductName = productName;
            Value.ProductSpec = ProductSpecText.Text.Trim();
            Value.OrderQuantity = OrderQuantityText.Text.Trim();
            Value.OrderType = OrderTypeCombo.Text.Trim();
            Value.InspectionItems = InspectionItemsText.Text.Trim();
            Value.InspectionStandard = InspectionStandardText.Text.Trim();
            Value.RequiredDate = RequiredDatePicker.SelectedDate;
            Value.ActualDate = ActualDatePicker.SelectedDate;
            Value.OrderStatus = OrderStatusCombo.Text.Trim();
            Value.InspectionFee = InspectionFeeText.Text.Trim();
            Value.PaymentStatus = PaymentStatusCombo.Text.Trim();
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static CommissionedOrderRecord Clone(CommissionedOrderRecord source)
        {
            return new CommissionedOrderRecord
            {
                Id = source.Id,
                OrderId = source.OrderId,
                OrderDate = source.OrderDate,
                CustomerName = source.CustomerName,
                ContactPerson = source.ContactPerson,
                ContactPhone = source.ContactPhone,
                ProductName = source.ProductName,
                ProductSpec = source.ProductSpec,
                OrderQuantity = source.OrderQuantity,
                OrderType = source.OrderType,
                InspectionItems = source.InspectionItems,
                InspectionStandard = source.InspectionStandard,
                RequiredDate = source.RequiredDate,
                ActualDate = source.ActualDate,
                OrderStatus = source.OrderStatus,
                InspectionFee = source.InspectionFee,
                PaymentStatus = source.PaymentStatus,
                Remark = source.Remark
            };
        }
    }
}
