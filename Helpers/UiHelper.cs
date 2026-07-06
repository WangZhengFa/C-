using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows;
using FoodEnterpriseIMS.Services;
using Newtonsoft.Json.Linq;

namespace FoodEnterpriseIMS.Helpers
{
    /// <summary>
    /// UI 辅助工具：窗口尺寸、图标等
    /// </summary>
    public static class UiHelper
    {
        /// <summary>
        /// 设置窗口图标
        /// </summary>
        public static void SetWindowIcon(Window window)
        {
            try
            {
                var image = new BitmapImage(new System.Uri("pack://application:,,,/Resources/Images/logo.png", System.UriKind.Absolute));
                window.Icon = image;
            }
            catch
            {
                try
                {
                    var file = Path.Combine(System.AppContext.BaseDirectory, "logo", "default_logo.png");
                    if (File.Exists(file))
                    {
                        window.Icon = new BitmapImage(new System.Uri(file, System.UriKind.Absolute));
                    }
                }
                catch
                {
                    // 图标加载失败时不阻塞窗口打开
                }
            }
        }

        /// <summary>
        /// 安全地应用窗口尺寸和位置
        /// </summary>
        public static void ApplySafeGeometry(Window window, Size defaultSize)
        {
            if (double.IsNaN(window.Width) || window.Width <= 0)
            {
                window.Width = defaultSize.Width;
            }

            if (double.IsNaN(window.Height) || window.Height <= 0)
            {
                window.Height = defaultSize.Height;
            }

            var workArea = SystemParameters.WorkArea;
            var width = System.Math.Max(window.MinWidth, System.Math.Min(window.Width, workArea.Width));
            var height = System.Math.Max(window.MinHeight, System.Math.Min(window.Height, workArea.Height));

            window.Width = width;
            window.Height = height;

            if (double.IsNaN(window.Left) || double.IsNaN(window.Top))
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return;
            }

            window.Left = System.Math.Max(workArea.Left, System.Math.Min(window.Left, workArea.Right - width));
            window.Top = System.Math.Max(workArea.Top, System.Math.Min(window.Top, workArea.Bottom - height));
        }

        /// <summary>
        /// 获取 UI 配置
        /// </summary>
        public static Dictionary<string, object>? GetUiConfig(DatabaseManager db)
        {
            try
            {
                var rows = db.GetSystemConfigList("ui");
                if (rows == null || rows.Count == 0)
                {
                    return null;
                }

                var result = new Dictionary<string, object>();
                foreach (var row in rows)
                {
                    var key = row.TryGetValue("config_key", out var keyObj) ? keyObj?.ToString() : null;
                    var value = row.TryGetValue("config_value", out var valueObj) ? valueObj?.ToString() : null;
                    if (string.IsNullOrWhiteSpace(key) || value == null)
                    {
                        continue;
                    }

                    if (key == "main_window_geometry")
                    {
                        if (TryParseWindowGeometry(value, out var geometry))
                        {
                            result[key] = geometry;
                        }
                    }
                    else if (key == "main_window_splitter_sizes")
                    {
                        if (TryParseSplitterSizes(value, out var sizes))
                        {
                            result[key] = sizes;
                        }
                    }
                    else
                    {
                        result[key] = value;
                    }
                }

                return result.Count > 0 ? result : null;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryParseWindowGeometry(string raw, out Dictionary<string, object> geometry)
        {
            geometry = new Dictionary<string, object>();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var text = raw.Trim();
            if (!text.StartsWith("{") || !text.EndsWith("}"))
            {
                return false;
            }

            try
            {
                if (JToken.Parse(text) is not JObject obj)
                {
                    return false;
                }

                geometry["x"] = ReadIntSafe(obj, "x", 0);
                geometry["y"] = ReadIntSafe(obj, "y", 0);
                geometry["w"] = ReadIntSafe(obj, "w", 1280);
                geometry["h"] = ReadIntSafe(obj, "h", 800);
                geometry["maximized"] = ReadBoolSafe(obj, "maximized", false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseSplitterSizes(string raw, out List<object> sizes)
        {
            sizes = new List<object>();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var text = raw.Trim();
            if (!text.StartsWith("[") || !text.EndsWith("]"))
            {
                return false;
            }

            try
            {
                if (JToken.Parse(text) is not JArray arr)
                {
                    return false;
                }

                foreach (var item in arr)
                {
                    sizes.Add(ReadIntElementSafe(item, 0));
                }

                return sizes.Count > 0;
            }
            catch
            {
                sizes.Clear();
                return false;
            }
        }

        private static int ReadIntSafe(JObject root, string propertyName, int fallback)
        {
            if (!root.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out var token))
            {
                return fallback;
            }

            return int.TryParse(token.ToString(), out var value) ? value : fallback;
        }

        private static int ReadIntElementSafe(JToken token, int fallback)
        {
            return int.TryParse(token.ToString(), out var value) ? value : fallback;
        }

        private static bool ReadBoolSafe(JObject root, string propertyName, bool fallback)
        {
            if (!root.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out var token))
            {
                return fallback;
            }

            return bool.TryParse(token.ToString(), out var value) ? value : fallback;
        }
    }
}
