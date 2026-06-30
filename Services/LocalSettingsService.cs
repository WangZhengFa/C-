using Newtonsoft.Json;
using System.IO;

namespace 食品信息管理系统.Services
{
    /// <summary>
    /// 本地配置服务：账号、密码、自动登录、版本号等使用本地 JSON 文件存储
    /// </summary>
    public static class LocalSettingsService
    {
        private static readonly string ConfigPath;
        private static readonly Dictionary<string, object> _settings;
        private static readonly Lock _lock = new();

        static LocalSettingsService()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "食品信息管理系统");
            Directory.CreateDirectory(dir);
            ConfigPath = Path.Combine(dir, "local_settings.json");
            _settings = LoadFromFile();
        }

        private static Dictionary<string, object> LoadFromFile()
        {
            if (!File.Exists(ConfigPath))
                return new Dictionary<string, object>();
            try
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        private static void SaveToFile()
        {
            lock (_lock)
            {
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(_settings, Formatting.Indented));
            }
        }

        private static T Get<T>(string key, T defaultValue)
        {
            lock (_lock)
            {
                if (_settings.TryGetValue(key, out var value) && value != null)
                {
                    try
                    {
                        if (value is T t) return t;
                        return (T)Convert.ChangeType(value, typeof(T))!;
                    }
                    catch { }
                }
                return defaultValue;
            }
        }

        private static void Set<T>(string key, T value)
        {
            lock (_lock)
            {
                _settings[key] = value!;
                SaveToFile();
            }
        }

        public static string SavedUsername
        {
            get => Get("SavedUsername", string.Empty);
            set => Set("SavedUsername", value);
        }

        public static string SavedPassword
        {
            get => Get("SavedPassword", string.Empty);
            set => Set("SavedPassword", value);
        }

        public static bool SaveUsernameChecked
        {
            get => Get("SaveUsernameChecked", false);
            set => Set("SaveUsernameChecked", value);
        }

        public static bool SavePasswordChecked
        {
            get => Get("SavePasswordChecked", false);
            set => Set("SavePasswordChecked", value);
        }

        public static bool AutoLoginChecked
        {
            get => Get("AutoLoginChecked", false);
            set => Set("AutoLoginChecked", value);
        }

        public static string LocalVersion
        {
            get => Get("LocalVersion", "1.0.0");
            set => Set("LocalVersion", value);
        }

        public static int AutoLoginCountdown
        {
            get => Get("AutoLoginCountdown", 3);
            set => Set("AutoLoginCountdown", value);
        }

        public static bool AutoUpdateEnabled
        {
            get => Get("AutoUpdateEnabled", true);
            set => Set("AutoUpdateEnabled", value);
        }

        public static void ClearLoginInfo()
        {
            SavedUsername = string.Empty;
            SavedPassword = string.Empty;
            SaveUsernameChecked = false;
            SavePasswordChecked = false;
            AutoLoginChecked = false;
        }
    }
}
