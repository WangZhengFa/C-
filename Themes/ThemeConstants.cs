using System;

namespace FoodEnterpriseIMS.Themes
{
    /// <summary>
    /// 主题类型枚举
    /// </summary>
    public enum ThemeType
    {
        Light,
        Dark
    }

    /// <summary>
    /// 控件角色枚举（对应原Qt的role属性）
    /// </summary>
    public enum ControlRole
    {
        Primary,
        Secondary,
        Danger,
        Success,
        Info,
        Warning,
        Error,
        Deleted,
        Muted,
        TableButton,
        TitleBar
    }

    /// <summary>
    /// 主题常量定义
    /// </summary>
    public static class ThemeConstants
    {
        // 颜色常量（对应原Qt QSS的颜色值）
        public static class DarkColors
        {
            public const string Background = "#121212";
            public const string Foreground = "#E0E0E0";
            public const string ControlBackground = "#222222";
            public const string Border = "#444444";
            public const string PrimaryButton = "#4CAF50";
            public const string SecondaryButton = "#1976d2";
            public const string DangerButton = "#F44336";
            public const string SuccessButton = "#2e7d32";
            public const string InfoButton = "#0288d1";
            public const string WarningButton = "#f9a825";
            public const string ErrorButton = "#c62828";
            public const string SelectedItem = "#1769c2";
            public const string HoverItem = "#2F3B4A";
            public const string Splitter = "#2C2C2C";
        }

        public static class LightColors
        {
            public const string Background = "#F2F2F2";
            public const string Foreground = "#222222";
            public const string ControlBackground = "#FFFFFF";
            public const string Border = "#CCCCCC";
            public const string PrimaryButton = "#4CAF50";
            public const string SecondaryButton = "#1976d2";
            public const string DangerButton = "#F44336";
            public const string SuccessButton = "#2e7d32";
            public const string InfoButton = "#0288d1";
            public const string WarningButton = "#f9a825";
            public const string ErrorButton = "#c62828";
            public const string SelectedItem = "#005BBB";
            public const string HoverItem = "#e5f0ff";
            public const string Splitter = "#CFCFCF";
        }

        // 字体常量
        public static class Fonts
        {
            public const string DefaultFontFamily = "Microsoft YaHei, Segoe UI, system-ui";
            public const double DefaultFontSize = 11;
            public const double ButtonFontSize = 9;
            public const double TableButtonFontSize = 12;
            public const double MutedFontSize = 8;
            public const double HeaderFontSize = 12;
        }

        // 尺寸常量
        public static class Sizes
        {
            public const double MinControlHeight = 26;
            public const double TableButtonHeight = 16;
            public const double IndicatorSize = 14;
            public const double LightIndicatorSize = 12;
            public const double TreeRowHeight = 32;  // 增加行高，避免内容被压扁
            public const double SplitterWidth = 1;
        }
    }
}