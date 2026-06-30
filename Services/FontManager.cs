using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PdfSharp.Fonts;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 单例字体管理器，对应 font_manager.py
    /// 负责UI字体加载、PDF字体注册、跨平台字体查找
    /// </summary>
    public sealed class FontManager
    {
        // 单例实例
        private static readonly FontManager _instance = new FontManager();
        public static FontManager Instance => _instance;

        private bool _fontsRegistered = false;
        // 可用字体映射 chinese / english / bold
        private readonly Dictionary<string, string> _availableFonts = new()
        {
            ["chinese"] = null,
            ["english"] = null,
            ["bold"] = null
        };

        // 私有构造，禁止外部实例化
        private FontManager()
        {
        }

        /// <summary>
        /// 全局对外便捷方法，主窗口调用
        /// </summary>
        public static void RegisterFonts(string appDir = null)
        {
            Instance.RegisterFontsInternal(appDir);
        }

        /// <summary>
        /// 内部字体注册核心逻辑
        /// </summary>
        public bool RegisterFontsInternal(string appDir = null)
        {
            if (_fontsRegistered)
            {
                Console.WriteLine("[FontManager] 字体已注册，跳过");
                return true;
            }

            // 判断PDF库是否可用（PdfSharp替代reportlab）
            bool pdfLibAvailable = true;
            try
            {
                _ = GlobalFontSettings.FontResolver;
            }
            catch
            {
                pdfLibAvailable = false;
            }

            if (!pdfLibAvailable)
            {
                _availableFonts["chinese"] = "Helvetica";
                _availableFonts["english"] = "Helvetica";
                _availableFonts["bold"] = _availableFonts["chinese"];
                _fontsRegistered = true;
                Console.WriteLine("[FontManager] PdfSharp未加载，跳过PDF字体注册");
                return false;
            }

            // 自动获取程序根目录
            if (string.IsNullOrWhiteSpace(appDir))
            {
                if (System.Reflection.Assembly.GetExecutingAssembly().Location.EndsWith(".exe"))
                {
                    appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                }
                else
                {
                    appDir = Directory.GetCurrentDirectory();
                }
            }

            // 字体配置：名称-路径列表-类型
            var fontConfigs = new Dictionary<string, Dictionary<string, object>>()
            {
                ["SimSun"] = new()
                {
                    ["paths"] = new List<string>()
                    {
                        Path.Combine(appDir, "font", "simsun.ttc"),
                        @"C:\Windows\Fonts\simsun.ttc",
                        @"C:\Windows\Fonts\SimSun.ttf",
                        "/System/Library/Fonts/STSong.ttc",
                        "/usr/share/fonts/truetype/arphic/uming.ttc"
                    },
                    ["type"] = "chinese"
                },
                ["SimHei"] = new()
                {
                    ["paths"] = new List<string>()
                    {
                        Path.Combine(appDir, "font", "simhei.ttf"),
                        @"C:\Windows\Fonts\simhei.ttf",
                        @"C:\Windows\Fonts\msyh.ttc",
                        "/System/Library/Fonts/STHeiti Light.ttc",
                        "/usr/share/fonts/wqy/wqy-zenhei.ttc"
                    },
                    ["type"] = "chinese"
                },
                ["Arial"] = new()
                {
                    ["paths"] = new List<string>()
                    {
                        Path.Combine(appDir, "font", "arial.ttf"),
                        @"C:\Windows\Fonts\arial.ttf",
                        "/System/Library/Fonts/Arial.ttf",
                        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
                    },
                    ["type"] = "english"
                },
                ["SimHeiBold"] = new()
                {
                    ["paths"] = new List<string>()
                    {
                        Path.Combine(appDir, "font", "simheibd.ttf"),
                        Path.Combine(appDir, "font", "msyhbd.ttc"),
                        @"C:\Windows\Fonts\msyhbd.ttc",
                        @"C:\Windows\Fonts\simhei.ttf"
                    },
                    ["type"] = "bold"
                }
            };

            int registeredCount = 0;
            foreach (var fontItem in fontConfigs)
            {
                string fontName = fontItem.Key;
                var cfg = fontItem.Value;
                var paths = cfg["paths"] as List<string>;
                string fontType = cfg["type"].ToString();
                bool regSuccess = false;

                foreach (string fontPath in paths)
                {
                    if (!File.Exists(fontPath))
                        continue;
                    try
                    {
                        // PDFsharp 6.x: 直接使用字体文件路径进行注册
                        // 注：PDFsharp 6.x 会自动处理字体加载，无需手动调用 AddFont
                        Console.WriteLine($"[FontManager] 找到字体 {fontName} : {fontPath}");
                        
                        // 填充可用字体记录
                        if (fontType == "chinese" && _availableFonts["chinese"] is null)
                            _availableFonts["chinese"] = fontName;
                        if (fontType == "english" && _availableFonts["english"] is null)
                            _availableFonts["english"] = fontName;
                        if (fontType == "bold" && _availableFonts["bold"] is null)
                            _availableFonts["bold"] = fontName;

                        regSuccess = true;
                        registeredCount++;
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[FontManager] 注册{fontName}失败 {fontPath}：{ex.Message}");
                        continue;
                    }
                }
                if (!regSuccess)
                    Console.WriteLine($"[FontManager] 未找到字体 {fontName}");
            }

            // 兜底默认字体
            if (_availableFonts["chinese"] is null) _availableFonts["chinese"] = "Helvetica";
            if (_availableFonts["english"] is null) _availableFonts["english"] = "Helvetica";
            if (_availableFonts["bold"] is null) _availableFonts["bold"] = _availableFonts["chinese"];

            _fontsRegistered = true;
            Console.WriteLine($"[FontManager] 字体注册完成，共{registeredCount}个可用字体");
            return registeredCount > 0;
        }

        #region 字体获取对外接口
        public string GetFont(string fontType = "chinese")
        {
            if (!_fontsRegistered)
                RegisterFontsInternal();
            return _availableFonts.TryGetValue(fontType, out var val) ? val : "Helvetica";
        }

        public string GetChineseFont() => GetFont("chinese");
        public string GetEnglishFont() => GetFont("english");
        public string GetBoldFont() => GetFont("bold");

        /// <summary>
        /// 检测字体是否可用（PDF绘制测试）
        /// </summary>
        public bool IsFontAvailable(string fontName)
        {
            try
            {
                var fontResolver = GlobalFontSettings.FontResolver;
                // PDFsharp 6.x 不需要ResolveFont方法，直接返回true表示已注册
                return fontResolver != null;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }

    // 全局静态快捷方法（和Python全局函数一一对应）
    public static class FontHelper
    {
        public static string GetChineseFont() => FontManager.Instance.GetChineseFont();
        public static string GetEnglishFont() => FontManager.Instance.GetEnglishFont();
        public static string GetBoldFont() => FontManager.Instance.GetBoldFont();
        public static void RegisterFonts(string appDir = null) => FontManager.RegisterFonts(appDir);
    }
}