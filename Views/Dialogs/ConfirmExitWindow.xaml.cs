using System;
using System.Windows;
using System.Windows.Threading;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 退出确认窗口，支持倒计时自动确认
    /// </summary>
    public partial class ConfirmExitWindow : Window
    {
        private int _secondsLeft = 30;
        private readonly DispatcherTimer _timer;
        public bool ConfirmExit { get; private set; }

        public ConfirmExitWindow()
        {
            InitializeComponent();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTimerTick;
            Loaded += (s, e) => _timer.Start();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            _secondsLeft--;
            CountdownText.Text = $"{_secondsLeft} 秒后自动退出";

            if (_secondsLeft <= 0)
            {
                _timer.Stop();
                ConfirmExit = true;
                DialogResult = true;
                Close();
            }
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            ConfirmExit = true;
            DialogResult = true;
            Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            ConfirmExit = false;
            DialogResult = false;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _timer.Stop();
        }
    }
}
