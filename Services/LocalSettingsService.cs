using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;

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
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new Dictionary<string, object>();
                }

                var token = JToken.Parse(json);
                if (token is not JObject obj)
                {
                    return new Dictionary<string, object>();
                }

                var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in obj.Properties())
                {
                    result[prop.Name] = ConvertJToken(prop.Value);
                }

                return result;
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        private static object ConvertJToken(JToken token)
        {
            return token.Type switch
            {
                JTokenType.String => token.ToString(),
                JTokenType.Integer => token.Value<long>(),
                JTokenType.Float => token.Value<double>(),
                JTokenType.Boolean => token.Value<bool>(),
                JTokenType.Array => token.Children().Select(ConvertJToken).ToList(),
                JTokenType.Object => ((JObject)token).Properties().ToDictionary(p => p.Name, p => ConvertJToken(p.Value), StringComparer.OrdinalIgnoreCase),
                _ => token.ToString()
            };
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

        private static List<string> GetStringList(string key)
        {
            lock (_lock)
            {
                if (!_settings.TryGetValue(key, out var value) || value == null)
                {
                    return new List<string>();
                }

                try
                {
                    if (value is List<string> list)
                    {
                        return list.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList();
                    }

                    if (value is Newtonsoft.Json.Linq.JArray arr)
                    {
                        return arr.Select(t => t?.ToString() ?? string.Empty)
                                  .Where(x => !string.IsNullOrWhiteSpace(x))
                                  .Select(x => x.Trim())
                                  .Distinct()
                                  .ToList();
                    }
                }
                catch
                {
                    // ignore
                }

                return new List<string>();
            }
        }

        private static void SetStringList(string key, IEnumerable<string> values)
        {
            lock (_lock)
            {
                _settings[key] = values.Where(x => !string.IsNullOrWhiteSpace(x))
                                       .Select(x => x.Trim())
                                       .Distinct()
                                       .ToList();
                SaveToFile();
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

        public static List<string> RecentUsernames
        {
            get => GetStringList("RecentUsernames");
            set => SetStringList("RecentUsernames", value ?? new List<string>());
        }

        public static List<string> RecentDbHosts
        {
            get => GetStringList("RecentDbHosts");
            set => SetStringList("RecentDbHosts", value ?? new List<string>());
        }

        public static List<string> RecentDbNames
        {
            get => GetStringList("RecentDbNames");
            set => SetStringList("RecentDbNames", value ?? new List<string>());
        }

        public static void AddUsernameHistory(string username)
        {
            var value = (username ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var list = RecentUsernames;
            list.RemoveAll(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
            list.Insert(0, value);
            if (list.Count > 10)
            {
                list = list.Take(10).ToList();
            }

            RecentUsernames = list;
        }

        public static void AddDbHostHistory(string host)
        {
            var value = (host ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var list = RecentDbHosts;
            list.RemoveAll(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
            list.Insert(0, value);
            if (list.Count > 10)
            {
                list = list.Take(10).ToList();
            }

            RecentDbHosts = list;
        }

        public static void AddDbNameHistory(string dbName)
        {
            var value = (dbName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var list = RecentDbNames;
            list.RemoveAll(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
            list.Insert(0, value);
            if (list.Count > 10)
            {
                list = list.Take(10).ToList();
            }

            RecentDbNames = list;
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
