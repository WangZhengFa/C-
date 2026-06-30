using System.Windows;
using System.Windows.Media;
using FoodEnterpriseIMS.Database;
using MySqlConnector;
using 食品信息管理系统.Services;

namespace 食品信息管理系统.Views
{
    /// <summary>
    /// 数据库连接配置窗口
    /// </summary>
    public partial class DbSettingsWindow : Window
    {
        public DbSettingsWindow()
        {
            InitializeComponent();
            LoadConfig();
        }

        private void LoadConfig()
        {
            var cfg = DbConfigService.LoadConfig();
            HostEntry.Text = cfg.Host;
            PortEntry.Text = cfg.Port.ToString();
            UserEntry.Text = cfg.User;
            PasswordEntry.Password = cfg.Password;
            DatabaseEntry.Text = cfg.Database;
            CountdownEntry.Text = LocalSettingsService.AutoLoginCountdown.ToString();
            AutoUpdateCheck.IsChecked = LocalSettingsService.AutoUpdateEnabled;
        }

        private MySqlConfig GetConfigFromUi()
        {
            return new MySqlConfig
            {
                Host = HostEntry.Text?.Trim() ?? "127.0.0.1",
                Port = int.TryParse(PortEntry.Text, out var port) ? port : 3306,
                User = UserEntry.Text?.Trim() ?? string.Empty,
                Password = PasswordEntry.Password ?? string.Empty,
                Database = DatabaseEntry.Text?.Trim() ?? "spzhprogram"
            };
        }

        private async void OnTestClicked(object sender, RoutedEventArgs e)
        {
            var cfg = GetConfigFromUi();
            var connString = MysqlDbInitializer.GetConnStringWithoutDb(cfg);

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();
                ShowMessage("连接成功", Brushes.Green);
            }
            catch (Exception ex)
            {
                ShowMessage($"连接失败：{ex.Message}", Brushes.Red);
            }
        }

        private void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            var cfg = GetConfigFromUi();
            DbConfigService.SaveConfig(cfg);

            int countdown = int.TryParse(CountdownEntry.Text, out var cd) ? Math.Clamp(cd, 1, 60) : 3;
            LocalSettingsService.AutoLoginCountdown = countdown;
            LocalSettingsService.AutoUpdateEnabled = AutoUpdateCheck.IsChecked == true;

            ShowMessage("设置已保存", Brushes.Green);
            _ = Task.Run(async () =>
            {
                await Task.Delay(800);
                await Dispatcher.InvokeAsync(Close);
            });
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowMessage(string message, Brush brush)
        {
            MessageLabel.Text = message;
            MessageLabel.Foreground = brush;
            MessageLabel.Visibility = Visibility.Visible;
        }
    }
}
