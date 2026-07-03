using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;
using 食品信息管理系统.Services;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 报告发放编辑窗口
    /// </summary>
    public partial class ReportIssuanceEditWindow : Window
    {
        public ReportDistributionRecord Value { get; private set; }

        public ReportIssuanceEditWindow(ReportDistributionRecord? source)
        {
            InitializeComponent();
            Value = source == null ? new ReportDistributionRecord { DistributionDate = DateTime.Today } : Clone(source);
            InitCombos();
            BindValue();
        }

        private void InitCombos()
        {
            var users = LocalSettingsService.RecentUsernames ?? new List<string>();
            FillCombo(DistributorCombo, users);
            FillCombo(RecipientCombo, users);
            FillCombo(AcceptorCombo, users);
        }

        private static void FillCombo(System.Windows.Controls.ComboBox combo, IEnumerable<string> values)
        {
            combo.Items.Clear();
            foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                combo.Items.Add(value);
            }
        }

        private void BindValue()
        {
            ReportCodeText.Text = Value.ReportCode;
            DistributionDatePicker.SelectedDate = Value.DistributionDate;
            DistributorCombo.Text = Value.Distributor;
            RecipientCombo.Text = Value.Recipient;
            ReceiveDatePicker.SelectedDate = Value.ReceiveDate;
            AcceptorCombo.Text = Value.Acceptor;
            IsReceivedCheck.IsChecked = Value.IsReceived;
            AcceptDatePicker.SelectedDate = Value.AcceptDate;
            IsAcceptedCheck.IsChecked = Value.IsAccepted;
            RemarkText.Text = Value.Remarks;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var reportCode = ReportCodeText.Text.Trim();
            if (string.IsNullOrWhiteSpace(reportCode))
            {
                MessageBox.Show(this, "报告编号不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.ReportCode = reportCode;
            Value.DistributionDate = DistributionDatePicker.SelectedDate;
            Value.Distributor = DistributorCombo.Text.Trim();
            Value.Recipient = RecipientCombo.Text.Trim();
            Value.ReceiveDate = ReceiveDatePicker.SelectedDate;
            Value.Acceptor = AcceptorCombo.Text.Trim();
            Value.IsReceived = IsReceivedCheck.IsChecked == true;
            Value.AcceptDate = AcceptDatePicker.SelectedDate;
            Value.IsAccepted = IsAcceptedCheck.IsChecked == true;
            Value.Remarks = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static ReportDistributionRecord Clone(ReportDistributionRecord source)
        {
            return new ReportDistributionRecord
            {
                Id = source.Id,
                ReportCode = source.ReportCode,
                DistributionDate = source.DistributionDate,
                Distributor = source.Distributor,
                Recipient = source.Recipient,
                ReceiveDate = source.ReceiveDate,
                Acceptor = source.Acceptor,
                IsReceived = source.IsReceived,
                AcceptDate = source.AcceptDate,
                IsAccepted = source.IsAccepted,
                Remarks = source.Remarks
            };
        }
    }
}
