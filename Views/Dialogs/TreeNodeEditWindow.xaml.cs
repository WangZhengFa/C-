using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FoodEnterpriseIMS.TreeCore;
using MySqlConnector;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 树节点编辑窗口
    /// </summary>
    public partial class TreeNodeEditWindow : Window
    {
        private TreeNode _node;
        private readonly string _treeKey;
        private bool _isNew;
        private readonly bool _isCSharpMode;
        private readonly Dictionary<string, object> _fullPayload = new();
        private readonly ObservableCollection<PayloadEntry> _displayEntries = new();
        private static readonly HashSet<string> CSharpKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "menu_key",
            "menu_path",
            "component_path",
            "csharp_class"
        };

        public bool Saved { get; private set; }

        public TreeNodeEditWindow(TreeNode node, string treeKey, bool isNew)
        {
            InitializeComponent();
            _node = node ?? throw new ArgumentNullException(nameof(node));
            _treeKey = treeKey;
            _isNew = isNew;
            _isCSharpMode = treeKey == "system_menu";

            LoadData();
        }

        /// <summary>
        /// 加载数据到界面
        /// </summary>
        private void LoadData()
        {
            if (_isNew)
            {
                GenerateNewCode();
            }

            ParentCodeText.Text = _node.ParentCode ?? "";
            CodeText.Text = _node.Code;
            TitleText.Text = _node.Title;
            SortOrderText.Text = _node.SortOrder.ToString();

            _fullPayload.Clear();
            if (_node.Payload != null)
            {
                foreach (var kv in _node.Payload)
                {
                    _fullPayload[kv.Key] = kv.Value;
                }
            }

            RefreshDisplayEntries();
        }

        /// <summary>
        /// 新增模式时自动生成当前编码
        /// </summary>
        private void GenerateNewCode()
        {
            try
            {
                var cfg = FoodEnterpriseIMS.Database.MysqlDbInitializer.LoadMysqlConfig();
                using var conn = new MySqlConnection(FoodEnterpriseIMS.Database.MysqlDbInitializer.GetConnString(cfg));
                conn.Open();
                var repo = new TreeRepository(conn, _treeKey);
                var allNodes = repo.ListNodes();
                var parentCode = _node.ParentCode ?? "";
                var siblings = allNodes
                    .Where(n => (n["parent_code"]?.ToString() ?? "") == parentCode)
                    .Select(n => n["code"]?.ToString() ?? "")
                    .Where(c => !string.IsNullOrEmpty(c));
                _node.Code = TreeCodeHelper.NextChildCode(parentCode, siblings);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成编码失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 根据当前模式刷新 DataGrid 显示
        /// </summary>
        private void RefreshDisplayEntries()
        {
            _displayEntries.Clear();
            foreach (var kv in _fullPayload)
            {
                if (ShouldDisplayKey(kv.Key))
                {
                    _displayEntries.Add(new PayloadEntry
                    {
                        Key = kv.Key,
                        Value = kv.Value?.ToString() ?? ""
                    });
                }
            }
            PayloadListBox.ItemsSource = _displayEntries;
        }

        private bool ShouldDisplayKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            if (!_isCSharpMode) return true;
            return CSharpKeys.Contains(key);
        }

        /// <summary>
        /// 添加或聚焦到指定键的条目
        /// </summary>
        private void AddOrFocusEntry(string key, string value)
        {
            var existing = _displayEntries.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                PayloadListBox.SelectedItem = existing;
                return;
            }

            _fullPayload[key] = value;
            if (ShouldDisplayKey(key))
            {
                var entry = new PayloadEntry { Key = key, Value = value };
                _displayEntries.Add(entry);
                PayloadListBox.SelectedItem = entry;
            }
        }

        #region 工具栏事件
        private void AddRowButton_Click(object sender, RoutedEventArgs e)
        {
            string newKey = "";
            if (_isCSharpMode)
            {
                foreach (var k in CSharpKeys)
                {
                    if (!_displayEntries.Any(x => string.Equals(x.Key, k, StringComparison.OrdinalIgnoreCase)))
                    {
                        newKey = k;
                        break;
                    }
                }
                if (string.IsNullOrEmpty(newKey))
                {
                    MessageBox.Show("C# 模式下所有预定义键已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            _fullPayload[newKey] = "";
            var entry = new PayloadEntry { Key = newKey, Value = "" };
            _displayEntries.Add(entry);
            PayloadListBox.SelectedItem = entry;
        }

        private void DeleteRowButton_Click(object sender, RoutedEventArgs e)
        {
            if (PayloadListBox.SelectedItem is not PayloadEntry entry) return;
            _displayEntries.Remove(entry);
            _fullPayload.Remove(entry.Key);
        }

        private void AddMenuPathButton_Click(object sender, RoutedEventArgs e)
        {
            AddOrFocusEntry("menu_path", "");
        }

        private void AddComponentPathButton_Click(object sender, RoutedEventArgs e)
        {
            AddOrFocusEntry("component_path", "");
        }
        #endregion

        #region 底部按钮事件
        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            _isNew = true;
            _node = new TreeNode { ParentCode = _node.ParentCode };
            LoadData();
            Saved = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CodeText.Text))
            {
                MessageBox.Show("请输入当前编码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(TitleText.Text))
            {
                MessageBox.Show("请输入节点标题", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _node.Code = CodeText.Text.Trim();
            _node.Title = TitleText.Text.Trim();
            _node.ParentCode = string.IsNullOrWhiteSpace(ParentCodeText.Text) ? null : ParentCodeText.Text.Trim();
            if (int.TryParse(SortOrderText.Text, out var sortOrder))
            {
                _node.SortOrder = sortOrder;
            }

            // 从显示条目重建 Payload，C# 模式下保留未显示的非 C# 键
            var newPayload = new Dictionary<string, object>();
            foreach (var entry in _displayEntries)
            {
                if (!string.IsNullOrWhiteSpace(entry.Key))
                {
                    newPayload[entry.Key] = entry.Value ?? "";
                }
            }
            if (_isCSharpMode)
            {
                foreach (var kv in _fullPayload)
                {
                    if (!CSharpKeys.Contains(kv.Key) && !newPayload.ContainsKey(kv.Key))
                    {
                        newPayload[kv.Key] = kv.Value;
                    }
                }
            }
            _node.Payload = newPayload;

            try
            {
                TreeCodeHelper.Validate(_node.Code);

                var cfg = FoodEnterpriseIMS.Database.MysqlDbInitializer.LoadMysqlConfig();
                using var conn = new MySqlConnection(FoodEnterpriseIMS.Database.MysqlDbInitializer.GetConnString(cfg));
                conn.Open();
                var repo = new TreeRepository(conn, _treeKey);
                repo.SaveNode(_node);
                Saved = true;
                MessageBox.Show("保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isNew)
            {
                LoadNodeFromDb();
            }
            else
            {
                _node = new TreeNode { ParentCode = _node.ParentCode };
                GenerateNewCode();
            }
            LoadData();
            Saved = false;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        #endregion

        /// <summary>
        /// 从数据库重新加载当前节点
        /// </summary>
        private void LoadNodeFromDb()
        {
            try
            {
                var cfg = FoodEnterpriseIMS.Database.MysqlDbInitializer.LoadMysqlConfig();
                using var conn = new MySqlConnection(FoodEnterpriseIMS.Database.MysqlDbInitializer.GetConnString(cfg));
                conn.Open();
                var repo = new TreeRepository(conn, _treeKey);
                var dict = repo.GetNode(_node.Code);
                if (dict != null)
                {
                    _node = new TreeNode
                    {
                        Code = dict["code"]?.ToString() ?? "",
                        Title = dict["title"]?.ToString() ?? "",
                        ParentCode = dict["parent_code"]?.ToString(),
                        SortOrder = Convert.ToInt32(dict["sort_order"] ?? 0),
                        Payload = dict["payload"] is Dictionary<string, object> pl ? pl : new Dictionary<string, object>()
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重新加载失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Payload 键值对显示项
    /// </summary>
    public class PayloadEntry
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
