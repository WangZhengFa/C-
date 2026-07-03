using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FoodEnterpriseIMS.Database;
using FoodEnterpriseIMS.Helpers;
using FoodEnterpriseIMS.Services;
using MySqlConnector;

namespace 食品信息管理系统.Views.Pages
{
    /// <summary>
    /// 数据分析页面
    /// </summary>
    public partial class DataAnalysisPage : Page
    {
        public event EventHandler? CloseRequested;

        private readonly DatabaseManager _db;
        private readonly int _currentRole;

        public DataAnalysisPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public DataAnalysisPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            LoadMetrics();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadMetrics();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void LoadMetrics()
        {
            try
            {
                UserCountText.Text = GetCount("user_accounts").ToString(CultureInfo.InvariantCulture);
                ProductCountText.Text = GetCount("product_infos").ToString(CultureInfo.InvariantCulture);
                MaterialCountText.Text = GetCount("material_infos").ToString(CultureInfo.InvariantCulture);
                CustomerCountText.Text = GetCount("customer_infos").ToString(CultureInfo.InvariantCulture);
                DocumentCountText.Text = GetCount("document_files").ToString(CultureInfo.InvariantCulture);
                ExamConfigCountText.Text = GetCount("exam_config").ToString(CultureInfo.InvariantCulture);
                QuestionCountText.Text = GetCount("exam_question_bank").ToString(CultureInfo.InvariantCulture);
                ReportDataCountText.Text = GetCount("report_data_main").ToString(CultureInfo.InvariantCulture);
                VersionLogCountText.Text = GetCount("version_log").ToString(CultureInfo.InvariantCulture);
                SystemConfigCountText.Text = GetCount("system_config").ToString(CultureInfo.InvariantCulture);

                var latestVersion = GetLatestVersion();
                LatestVersionText.Text = string.IsNullOrWhiteSpace(latestVersion.version) ? "-" : latestVersion.version;
                LatestVersionDateText.Text = string.IsNullOrWhiteSpace(latestVersion.updateDate) ? string.Empty : $"更新日期：{latestVersion.updateDate}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载数据分析失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private long GetCount(string tableName)
        {
            var cfg = MysqlDbInitializer.LoadMysqlConfig();
            var connString = MysqlDbInitializer.GetConnString(cfg);
            using var conn = new MySqlConnection(connString);
            conn.Open();
            using var cmd = new MySqlCommand($"SELECT COUNT(*) FROM {tableName}", conn);
            var result = cmd.ExecuteScalar();
            return result == null ? 0 : Convert.ToInt64(result);
        }

        private (string version, string updateDate) GetLatestVersion()
        {
            var cfg = MysqlDbInitializer.LoadMysqlConfig();
            var connString = MysqlDbInitializer.GetConnString(cfg);
            using var conn = new MySqlConnection(connString);
            conn.Open();
            const string sql = "SELECT version, update_date FROM version_log ORDER BY update_date DESC, id DESC LIMIT 1";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return (string.Empty, string.Empty);
            }

            var version = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            var updateDate = reader.IsDBNull(1) ? string.Empty : reader.GetDateTime(1).ToString("yyyy-MM-dd");
            return (version, updateDate);
        }
    }
}
