using System;
using System.IO;
using System.Windows;
using 食品信息管理系统.Views;

namespace FoodEnterpriseIMS
{
    /// <summary>
    /// WPF 应用程序入口
    /// </summary>
    public partial class App : Application
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 设置全局异常处理
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            WriteLog("=== 应用程序启动 ===");
            
            try
            {
                var login = new LoginWindow();
                login.Show();
                WriteLog("登录窗口已显示");
            }
            catch (Exception ex)
            {
                WriteLog($"显示登录窗口失败: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"程序启动失败:\n{ex.Message}\n\n详细信息请查看 app.log", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            WriteLog($"UI线程未处理异常: {e.Exception.Message}\n{FormatException(e.Exception)}");
            MessageBox.Show($"发生未处理的错误:\n{e.Exception.Message}\n\n详细信息请查看 app.log", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            WriteLog($"非UI线程未处理异常: {exception?.Message}\n{FormatException(exception)}");
            if (exception != null)
            {
                MessageBox.Show($"发生严重错误:\n{exception.Message}\n\n详细信息请查看 app.log", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string FormatException(Exception? ex)
        {
            if (ex == null) return "";
            var sb = new System.Text.StringBuilder();
            int depth = 0;
            while (ex != null && depth < 5)
            {
                sb.AppendLine($"--- 异常层级 {depth} ---");
                sb.AppendLine($"类型: {ex.GetType().FullName}");
                sb.AppendLine($"消息: {ex.Message}");
                sb.AppendLine("堆栈:");
                sb.AppendLine(ex.StackTrace);
                ex = ex.InnerException;
                depth++;
            }
            return sb.ToString();
        }

        internal static void WriteLog(string message)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFilePath, logEntry);
                Console.WriteLine(logEntry.Trim());
            }
            catch
            {
                // 如果写入日志失败，至少输出到控制台
                Console.WriteLine($"[LOG ERROR] {message}");
            }
        }
    }
}
