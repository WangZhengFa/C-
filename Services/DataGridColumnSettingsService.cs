using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// DataGrid列可见性设置服务（存储于system_config）。
    /// </summary>
    public class DataGridColumnSettingsService
    {
        private readonly DatabaseManager _db;
        private readonly string _configType;

        public DataGridColumnSettingsService(DatabaseManager db, string configType = "ui")
        {
            _db = db;
            _configType = configType;
        }

        public void Apply(DataGrid grid, string settingKey, Func<DataGridColumn, string> keySelector)
        {
            if (grid == null)
            {
                return;
            }

            var hidden = GetHiddenKeys(settingKey);
            foreach (var column in grid.Columns)
            {
                var key = keySelector(column)?.Trim() ?? string.Empty;
                column.Visibility = hidden.Contains(key) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public void Save(string settingKey, IEnumerable<string> hiddenColumnKeys)
        {
            var value = string.Join(";", hiddenColumnKeys
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase));
            _db.SetSystemConfig(_configType, settingKey, value);
        }

        public HashSet<string> GetHiddenKeys(string settingKey)
        {
            var raw = _db.GetSystemConfig($"{_configType}:{settingKey}") ?? string.Empty;
            return new HashSet<string>(
                raw.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x)),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
