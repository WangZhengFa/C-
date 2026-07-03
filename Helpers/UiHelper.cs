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
                        var geoToken = JToken.Parse(value);
                        if (geoToken is JObject geo)
                        {
                            result[key] = new Dictionary<string, object>
                            {
                                ["x"] = geo.Value<int?>("x") ?? 0,
                                ["y"] = geo.Value<int?>("y") ?? 0,
                                ["w"] = geo.Value<int?>("w") ?? 1280,
                                ["h"] = geo.Value<int?>("h") ?? 800,
                                ["maximized"] = geo.Value<bool?>("maximized") ?? false
                            };
                        }
                    }
                    else if (key == "main_window_splitter_sizes")
                    {
                        var splitToken = JToken.Parse(value);
                        if (splitToken is JArray splitArray)
                        {
                            var sizes = new List<object>();
                            foreach (var item in splitArray)
                            {
                                sizes.Add(item.Value<int>());
                            }
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
    }
}
