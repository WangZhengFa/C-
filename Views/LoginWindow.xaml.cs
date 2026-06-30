using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FoodEnterpriseIMS;
using 食品信息管理系统.Services;

namespace 食品信息管理系统.Views
{
    /// <summary>
    /// 登录窗口
    /// </summary>
    public partial class LoginWindow : Window
    {
        private bool _isAutoLoginInProgress;
        private bool _isLoggingIn;
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _countdownTimer;
        private int _countdownSeconds;

        public LoginWindow()
        {
            InitializeComponent();

            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) => UpdateStatusBar();
            _clockTimer.Start();

            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _countdownTimer.Tick += CountdownTimer_Tick;

            Loaded += LoginWindow_Loaded;
        }

        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                WriteLog("LoginWindow 加载完成");
                LoadSavedSettings();
                await CheckVersionAsync();
                WriteLog("登录窗口初始化完成");
            }
            catch (Exception ex)
            {
                WriteLog($"LoginWindow 加载失败: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static void WriteLog(string message)
        {
            try
            {
                var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [LoginWindow] {message}{Environment.NewLine}";
                System.IO.File.AppendAllText(logPath, logEntry);
                Console.WriteLine(logEntry.Trim());
            }
            catch
            {
                // 忽略日志写入错误
            }
        }

        /// <summary>
        /// 加载本地保存的账号、密码、自动登录等设置
        /// </summary>
        private async void LoadSavedSettings()
        {
            UsernameEntry.Text = LocalSettingsService.SavedUsername;
            SaveUsernameCheck.IsChecked = LocalSettingsService.SaveUsernameChecked;
            SavePasswordCheck.IsChecked = LocalSettingsService.SavePasswordChecked;
            AutoLoginCheck.IsChecked = LocalSettingsService.AutoLoginChecked;

            if (SavePasswordCheck.IsChecked == true)
            {
                PasswordEntry.Password = LocalSettingsService.SavedPassword;
            }

            UpdateStatusBar();

            if (AutoLoginCheck.IsChecked == true &&
                !string.IsNullOrWhiteSpace(UsernameEntry.Text) &&
                !string.IsNullOrWhiteSpace(PasswordEntry.Password))
            {
                _isAutoLoginInProgress = true;
                _countdownSeconds = LocalSettingsService.AutoLoginCountdown;
                CountdownLabel.Visibility = Visibility.Visible;
                CountdownLabel.Text = $"{_countdownSeconds}";
                _countdownTimer.Start();
            }
        }

        /// <summary>
        /// 登录倒计时
        /// </summary>
        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            _countdownSeconds--;
            if (_countdownSeconds <= 0)
            {
                _countdownTimer.Stop();
                CountdownLabel.Visibility = Visibility.Collapsed;
                _ = PerformLoginAsync();
            }
            else
            {
                CountdownLabel.Text = $"{_countdownSeconds}";
            }
        }

        /// <summary>
        /// 更新状态栏显示
        /// </summary>
        private void UpdateStatusBar()
        {
            VersionLabel.Text = $"版本号: {LocalSettingsService.LocalVersion}";
            ModeLabel.Text = "MySQL 模式: 10";
            TimeLabel.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 检查数据库最新版本号，是否需要更新
        /// </summary>
        private async Task CheckVersionAsync()
        {
            var latest = await VersionService.GetLatestVersionAsync();
            if (!string.IsNullOrWhiteSpace(latest) && VersionService.NeedUpdate(latest))
            {
                var result = MessageBox.Show(
                    $"当前版本：{LocalSettingsService.LocalVersion}\n最新版本：{latest}\n是否更新到最新版本？",
                    "发现新版本",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    LocalSettingsService.LocalVersion = latest;
                    UpdateStatusBar();
                }
            }
        }

        private void OnUsernameChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // 可在此扩展账号历史下拉功能
        }

        private void OnSaveUsernameChanged(object sender, RoutedEventArgs e)
        {
            if (SaveUsernameCheck.IsChecked != true)
            {
                SavePasswordCheck.IsChecked = false;
                AutoLoginCheck.IsChecked = false;
            }
        }

        private void OnSavePasswordChanged(object sender, RoutedEventArgs e)
        {
            if (SavePasswordCheck.IsChecked == true && SaveUsernameCheck.IsChecked != true)
            {
                SaveUsernameCheck.IsChecked = true;
            }
            if (SavePasswordCheck.IsChecked != true)
            {
                AutoLoginCheck.IsChecked = false;
            }
        }

        private async void OnLoginClicked(object sender, RoutedEventArgs e)
        {
            await PerformLoginAsync();
        }

        private async Task PerformLoginAsync()
        {
            if (_isLoggingIn) return;
            _isLoggingIn = true;

            try
            {
                var username = UsernameEntry.Text?.Trim() ?? string.Empty;
                var password = PasswordEntry.Password ?? string.Empty;

                SetControlsEnabled(false);
                MessageLabel.Visibility = Visibility.Collapsed;

                var (success, message, nickname, roleId) = await AuthService.ValidateAsync(username, password);

                if (success)
                {
                    SaveLoginSettings(username, password);

                    var displayName = !string.IsNullOrEmpty(nickname) ? nickname : username;
                    var config = new Dictionary<string, object>
                    {
                        ["user_name"] = displayName,
                        ["username"] = username,
                        ["role_id"] = roleId
                    };
                    var mainWindow = new MainAppWindow(config);
                    mainWindow.Show();
                    Close();
                }
                else
                {
                    ShowMessage(message, Brushes.Red);
                    _countdownTimer.Stop();
                    CountdownLabel.Visibility = Visibility.Collapsed;
                }

                _isAutoLoginInProgress = false;
            }
            finally
            {
                _isLoggingIn = false;
                SetControlsEnabled(true);
            }
        }

        /// <summary>
        /// 根据复选框状态保存登录信息
        /// </summary>
        private void SaveLoginSettings(string username, string password)
        {
            LocalSettingsService.SaveUsernameChecked = SaveUsernameCheck.IsChecked == true;
            LocalSettingsService.SavePasswordChecked = SavePasswordCheck.IsChecked == true;
            LocalSettingsService.AutoLoginChecked = AutoLoginCheck.IsChecked == true;

            if (SaveUsernameCheck.IsChecked == true)
            {
                LocalSettingsService.SavedUsername = username;
            }
            else
            {
                LocalSettingsService.SavedUsername = string.Empty;
            }

            if (SavePasswordCheck.IsChecked == true)
            {
                LocalSettingsService.SavedPassword = password;
            }
            else
            {
                LocalSettingsService.SavedPassword = string.Empty;
            }
        }

        private void OnSettingsClicked(object sender, RoutedEventArgs e)
        {
            var settings = new DbSettingsWindow();
            settings.Owner = this;
            settings.ShowDialog();
        }

        private void OnExitClicked(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ShowMessage(string message, Brush brush)
        {
            MessageLabel.Text = message;
            MessageLabel.Foreground = brush;
            MessageLabel.Visibility = Visibility.Visible;
        }

        private void SetControlsEnabled(bool enabled)
        {
            UsernameEntry.IsEnabled = enabled;
            PasswordEntry.IsEnabled = enabled;
            SaveUsernameCheck.IsEnabled = enabled;
            SavePasswordCheck.IsEnabled = enabled;
            AutoLoginCheck.IsEnabled = enabled;
            LoginBtn.IsEnabled = enabled;
            SettingsBtn.IsEnabled = enabled;
            ExitBtn.IsEnabled = enabled;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch { }
            }
        }
    }
}
