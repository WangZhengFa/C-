using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoodEnterpriseIMS.Helpers
{
    /// <summary>
    /// 天气信息管理辅助
    /// </summary>
    public static class WeatherHelper
    {
        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private static string _city = "北京";

        private sealed class WeatherSnapshot
        {
            public string City { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public string TemperatureText { get; init; } = string.Empty;
            public DateTime UpdatedAt { get; init; } = DateTime.Now;
        }

        public static event Action<object>? WeatherUpdated;
        public static event Action<string>? WeatherError;

        /// <summary>
        /// 设置天气城市
        /// </summary>
        public static void SetCity(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                WeatherError?.Invoke("城市名称不能为空");
                return;
            }

            _city = city.Trim();
        }

        /// <summary>
        /// 刷新天气信息
        /// </summary>
        public static void RefreshWeather()
        {
            _ = RefreshWeatherInternalAsync();
        }

        /// <summary>
        /// 格式化天气显示文本
        /// </summary>
        public static string FormatWeatherDisplay(object data)
        {
            if (data is WeatherSnapshot snapshot)
            {
                return $"{snapshot.City} {snapshot.TemperatureText} {snapshot.Description}";
            }

            return data?.ToString() ?? "🌤️ 加载中...";
        }

        private static async Task RefreshWeatherInternalAsync()
        {
            try
            {
                var url = $"https://wttr.in/{Uri.EscapeDataString(_city)}?format=%t|%C";
                var text = (await Http.GetStringAsync(url).ConfigureAwait(false)).Trim();
                var parts = text.Split('|', 2, StringSplitOptions.TrimEntries);
                var temperature = parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : "--°C";
                var weatherDesc = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : "--";

                var snapshot = new WeatherSnapshot
                {
                    City = _city,
                    Description = weatherDesc,
                    TemperatureText = temperature,
                    UpdatedAt = DateTime.Now
                };

                WeatherUpdated?.Invoke(snapshot);
            }
            catch (Exception ex)
            {
                var fallback = new WeatherSnapshot
                {
                    City = _city,
                    Description = "天气不可用",
                    TemperatureText = "--°C",
                    UpdatedAt = DateTime.Now
                };

                WeatherUpdated?.Invoke(fallback);
                WeatherError?.Invoke($"天气刷新失败: {ex.Message}");
            }
        }
    }
}
