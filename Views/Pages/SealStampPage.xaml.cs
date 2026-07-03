using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FoodEnterpriseIMS.Helpers;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;
using Microsoft.Win32;

namespace 食品信息管理系统.Views.Pages
{
    /// <summary>
    /// 加盖公章页面
    /// </summary>
    public partial class SealStampPage : Page
    {
        public event EventHandler? CloseRequested;

        private readonly SealStampService _service;
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly List<SealStampSettingRecord> _settings = new();

        public SealStampPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public SealStampPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            _service = new SealStampService();

            LoadSettings();
        }

        private void LoadSettings()
        {
            _settings.Clear();
            _settings.AddRange(_service.ListAll(_db));

            EnabledCheck.IsChecked = ReadValue("enabled") == "1";
            ImagePathText.Text = ReadValue("image_path");
            OpacityText.Text = ReadValue("opacity", "0.5");
            OffsetXText.Text = ReadValue("offset_x", "0");
            OffsetYText.Text = ReadValue("offset_y", "0");
            UpdatePreview();
        }

        private string ReadValue(string key, string defaultValue = "")
        {
            return _settings.FirstOrDefault(x => string.Equals(x.ConfigKey, key, StringComparison.OrdinalIgnoreCase))?.Value ?? defaultValue;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryValidateOpacity(out var opacity))
            {
                MessageBox.Show("透明度必须是 0 到 1 之间的小数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(OffsetXText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var offsetX))
            {
                MessageBox.Show("水平偏移必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(OffsetYText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var offsetY))
            {
                MessageBox.Show("垂直偏移必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var imagePath = ImagePathText.Text.Trim();
            if (!string.IsNullOrWhiteSpace(imagePath) && !File.Exists(imagePath))
            {
                MessageBox.Show("公章图片文件不存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var values = new[]
            {
                new SealStampSettingRecord { ConfigKey = "enabled", Value = EnabledCheck.IsChecked == true ? "1" : "0" },
                new SealStampSettingRecord { ConfigKey = "image_path", Value = imagePath },
                new SealStampSettingRecord { ConfigKey = "opacity", Value = opacity.ToString(CultureInfo.InvariantCulture) },
                new SealStampSettingRecord { ConfigKey = "offset_x", Value = offsetX.ToString(CultureInfo.InvariantCulture) },
                new SealStampSettingRecord { ConfigKey = "offset_y", Value = offsetY.ToString(CultureInfo.InvariantCulture) }
            };

            try
            {
                _service.SaveAll(_db, values);
                LoadSettings();
                MessageBox.Show("公章配置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存公章配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*",
                Title = "选择公章图片"
            };

            if (dialog.ShowDialog() == true)
            {
                ImagePathText.Text = dialog.FileName;
                UpdatePreview();
            }
        }

        private void UpdatePreview()
        {
            var imagePath = ImagePathText.Text.Trim();
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                PreviewImage.Source = null;
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.EndInit();
                PreviewImage.Source = bitmap;
            }
            catch
            {
                PreviewImage.Source = null;
            }
        }

        private bool TryValidateOpacity(out decimal opacity)
        {
            opacity = 0;
            var text = OpacityText.Text.Trim();
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out opacity))
            {
                return false;
            }

            return opacity >= 0m && opacity <= 1m;
        }
    }
}
