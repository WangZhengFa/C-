using System.Collections.Generic;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 窗口几何坐标配置
    /// </summary>
    public class WindowGeometryModel
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public bool Maximized { get; set; }
    }

    /// <summary>
    /// UI全局配置模型，对应Python _get_ui_config/_save_ui_config
    /// </summary>
    public class UiConfigModel
    {
        /// <summary>
        /// 主窗口位置大小
        /// </summary>
        public WindowGeometryModel MainWindowGeometry { get; set; } = new WindowGeometryModel();

        /// <summary>
        /// 分割器尺寸数组
        /// </summary>
        public List<int> MainWindowSplitterSizes { get; set; } = new List<int>();

        /// <summary>
        /// 从字典反序列化填充模型
        /// </summary>
        public static UiConfigModel FromDict(Dictionary<string, object> dict)
        {
            var model = new UiConfigModel();
            if (dict == null) return model;

            // 解析窗口几何
            if (dict.TryGetValue("main_window_geometry", out var geoObj) && geoObj is Dictionary<string, object> geoDict)
            {
                model.MainWindowGeometry.X = Convert.ToInt32(geoDict.GetValueOrDefault("x", 0));
                model.MainWindowGeometry.Y = Convert.ToInt32(geoDict.GetValueOrDefault("y", 0));
                model.MainWindowGeometry.W = Convert.ToInt32(geoDict.GetValueOrDefault("w", 1200));
                model.MainWindowGeometry.H = Convert.ToInt32(geoDict.GetValueOrDefault("h", 800));
                model.MainWindowGeometry.Maximized = Convert.ToBoolean(geoDict.GetValueOrDefault("maximized", false));
            }

            // 解析分割器尺寸
            if (dict.TryGetValue("main_window_splitter_sizes", out var splitObj) && splitObj is List<object> splitList)
            {
                foreach (var s in splitList)
                    model.MainWindowSplitterSizes.Add(Convert.ToInt32(s));
            }
            return model;
        }

        /// <summary>
        /// 模型转字典用于入库序列化
        /// </summary>
        public Dictionary<string, object> ToDict()
        {
            var dict = new Dictionary<string, object>();
            dict["main_window_geometry"] = new Dictionary<string, object>()
            {
                { "x", MainWindowGeometry.X },
                { "y", MainWindowGeometry.Y },
                { "w", MainWindowGeometry.W },
                { "h", MainWindowGeometry.H },
                { "maximized", MainWindowGeometry.Maximized }
            };
            dict["main_window_splitter_sizes"] = MainWindowSplitterSizes;
            return dict;
        }
    }
}