using System.IO;
using System.Text;
using FoodEnterpriseIMS.Database;

namespace 食品信息管理系统.Services
{
    /// <summary>
    /// 数据库连接配置读写服务，基于 launcher_config.ini
    /// </summary>
    public static class DbConfigService
    {
        private static string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher_config.ini");

        /// <summary>
        /// 加载数据库配置
        /// </summary>
        public static MySqlConfig LoadConfig()
        {
            return MysqlDbInitializer.LoadMysqlConfig();
        }

        /// <summary>
        /// 保存数据库配置到 ini 文件
        /// </summary>
        public static void SaveConfig(MySqlConfig config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[MySQL]");
            sb.AppendLine($"host = {config.Host}");
            sb.AppendLine($"port = {config.Port}");
            sb.AppendLine($"database = {config.Database}");
            sb.AppendLine($"user = {config.User}");
            sb.AppendLine($"password = {config.Password}");
            sb.AppendLine();

            sb.AppendLine("[Settings]");
            sb.AppendLine($"mysql_host = {config.Host}");
            sb.AppendLine($"mysql_port = {config.Port}");
            sb.AppendLine($"mysql_user = {config.User}");
            sb.AppendLine($"mysql_password = {config.Password}");
            sb.AppendLine($"db_name = {config.Database}");

            File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
        }
    }
}
