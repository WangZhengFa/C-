using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            InitNumericCombos();
            InitGeneralCombos();
            Loaded += (_, _) =>
            {
                AttachNumericPasteHandler(PortEntry);
                AttachNumericPasteHandler(CountdownEntry);
            };
            LoadConfig();
        }

        private void InitGeneralCombos()
        {
            HostEntry.Items.Clear();
            HostEntry.Items.Add("127.0.0.1");
            HostEntry.Items.Add("localhost");
            foreach (var host in LocalSettingsService.RecentDbHosts)
            {
                if (!HostEntry.Items.Contains(host))
                {
                    HostEntry.Items.Add(host);
                }
            }

            DatabaseEntry.Items.Clear();
            DatabaseEntry.Items.Add("spzhprogram");
            foreach (var dbName in LocalSettingsService.RecentDbNames)
            {
                if (!DatabaseEntry.Items.Contains(dbName))
                {
                    DatabaseEntry.Items.Add(dbName);
                }
            }
        }

        private void InitNumericCombos()
        {
            PortEntry.Items.Clear();
            PortEntry.Items.Add("3306");
            PortEntry.Items.Add("3307");
            PortEntry.Items.Add("13306");

            CountdownEntry.Items.Clear();
            CountdownEntry.Items.Add("3");
            CountdownEntry.Items.Add("5");
            CountdownEntry.Items.Add("10");
            CountdownEntry.Items.Add("15");
            CountdownEntry.Items.Add("30");
            CountdownEntry.Items.Add("60");
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
            var port = ParsePortOrDefault();
            return new MySqlConfig
            {
                Host = HostEntry.Text?.Trim() ?? "127.0.0.1",
                Port = port,
                User = UserEntry.Text?.Trim() ?? string.Empty,
                Password = PasswordEntry.Password ?? string.Empty,
                Database = DatabaseEntry.Text?.Trim() ?? "spzhprogram"
            };
        }

        private async void OnTestClicked(object sender, RoutedEventArgs e)
        {
            if (!TryValidatePort(out var port))
            {
                ShowMessage("端口必须是 1~65535 的整数", Brushes.Red);
                return;
            }

            var cfg = GetConfigFromUi();
            cfg.Port = port;
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
            if (!TryValidatePort(out var port))
            {
                ShowMessage("端口必须是 1~65535 的整数", Brushes.Red);
                return;
            }

            if (!TryValidateCountdown(out var countdown))
            {
                ShowMessage("自动登录倒计时必须是 1~60 的整数", Brushes.Red);
                return;
            }

            var cfg = GetConfigFromUi();
            cfg.Port = port;
            DbConfigService.SaveConfig(cfg);
            LocalSettingsService.AddDbHostHistory(cfg.Host);
            LocalSettingsService.AddDbNameHistory(cfg.Database);

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

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsDigits(e.Text);
        }

        private static bool IsDigits(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            foreach (var ch in text)
            {
                if (!char.IsDigit(ch))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryValidatePort(out int port)
        {
            if (!int.TryParse(PortEntry.Text?.Trim(), out port))
            {
                return false;
            }

            return port >= 1 && port <= 65535;
        }

        private int ParsePortOrDefault()
        {
            return TryValidatePort(out var port) ? port : 3306;
        }

        private bool TryValidateCountdown(out int countdown)
        {
            if (!int.TryParse(CountdownEntry.Text?.Trim(), out countdown))
            {
                return false;
            }

            return countdown >= 1 && countdown <= 60;
        }

        private static void AttachNumericPasteHandler(ComboBox combo)
        {
            combo.ApplyTemplate();
            if (combo.Template.FindName("PART_EditableTextBox", combo) is TextBox textBox)
            {
                DataObject.AddPastingHandler(textBox, OnNumericOnlyPaste);
            }
        }

        private static void OnNumericOnlyPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var text = e.DataObject.GetData(DataFormats.Text) as string;
            if (!IsDigits(text))
            {
                e.CancelCommand();
            }
        }
    }
}
