using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using FoodEnterpriseIMS.Helpers;
using FoodEnterpriseIMS.Models;
using FoodEnterpriseIMS.Services;
using 食品信息管理系统.Views.Dialogs;

namespace 食品信息管理系统.Views.Pages
{
    /// <summary>
    /// 考试题库页面
    /// </summary>
    public partial class ExamQuestionBankPage : Page
    {
        public event EventHandler? CloseRequested;

        private readonly ExamQuestionBankService _service;
        private readonly DatabaseManager _db;
        private readonly int _currentRole;
        private readonly ObservableCollection<ExamQuestionBankRecord> _records = new();
        private readonly ICollectionView _recordView;

        public ExamQuestionBankPage()
            : this(0, new DatabaseManager("FoodEnterpriseIMS.db"))
        {
        }

        public ExamQuestionBankPage(int currentRole, DatabaseManager db)
        {
            InitializeComponent();
            _currentRole = currentRole;
            _db = db;
            _service = new ExamQuestionBankService();

            _recordView = CollectionViewSource.GetDefaultView(_records);
            _recordView.Filter = RecordFilter;
            RecordGrid.ItemsSource = _recordView;

            InitFilterOptions();
            ApplyButtonPermissions();
            LoadRecords();
        }

        private void InitFilterOptions()
        {
            QuestionTypeFilterCombo.Items.Clear();
            QuestionTypeFilterCombo.Items.Add(string.Empty);
            QuestionTypeFilterCombo.Items.Add("单选题");
            QuestionTypeFilterCombo.Items.Add("多选题");
            QuestionTypeFilterCombo.Items.Add("判断题");

            DifficultyFilterCombo.Items.Clear();
            DifficultyFilterCombo.Items.Add(string.Empty);
            DifficultyFilterCombo.Items.Add("简单");
            DifficultyFilterCombo.Items.Add("中等");
            DifficultyFilterCombo.Items.Add("困难");
        }

        private void LoadRecords()
        {
            _records.Clear();
            foreach (var item in _service.ListAll())
            {
                _records.Add(item);
            }

            var currentType = QuestionTypeFilterCombo.Text?.Trim() ?? string.Empty;
            var currentDifficulty = DifficultyFilterCombo.Text?.Trim() ?? string.Empty;
            var types = _records.Select(x => x.QuestionType)
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .Distinct()
                                .OrderBy(x => x)
                                .ToList();
            var difficulties = _records.Select(x => x.Difficulty)
                                       .Where(x => !string.IsNullOrWhiteSpace(x))
                                       .Distinct()
                                       .OrderBy(x => x)
                                       .ToList();
            InitFilterOptions();
            foreach (var type in types)
            {
                if (!QuestionTypeFilterCombo.Items.Contains(type))
                {
                    QuestionTypeFilterCombo.Items.Add(type);
                }
            }
            foreach (var difficulty in difficulties)
            {
                if (!DifficultyFilterCombo.Items.Contains(difficulty))
                {
                    DifficultyFilterCombo.Items.Add(difficulty);
                }
            }
            QuestionTypeFilterCombo.Text = currentType;
            DifficultyFilterCombo.Text = currentDifficulty;

            _recordView.Refresh();
        }

        private bool RecordFilter(object item)
        {
            if (item is not ExamQuestionBankRecord record)
            {
                return false;
            }

            var keyword = KeywordFilterText.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var hit = Contains(record.QuestionType, keyword)
                          || Contains(record.QuestionContent, keyword)
                          || Contains(record.OptionsJson, keyword)
                          || Contains(record.Answer, keyword)
                          || Contains(record.Analysis, keyword)
                          || Contains(record.Category, keyword)
                          || Contains(record.Remark, keyword);
                if (!hit)
                {
                    return false;
                }
            }

            var type = QuestionTypeFilterCombo.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(type) && !string.Equals(record.QuestionType ?? string.Empty, type, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var difficulty = DifficultyFilterCombo.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(difficulty) && !string.Equals(record.Difficulty ?? string.Empty, difficulty, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (EnabledOnlyCheck.IsChecked == true && !record.IsEnabled)
            {
                return false;
            }

            return true;
        }

        private static bool Contains(string? text, string keyword)
        {
            return (text ?? string.Empty).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ExamQuestionBankEditWindow(null, _records) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _service.Insert(dialog.Value);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"新增题库失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not ExamQuestionBankRecord selected)
            {
                MessageBox.Show("请选择一条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new ExamQuestionBankEditWindow(selected, _records) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                dialog.Value.Id = selected.Id;
                _service.Update(dialog.Value);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新题库失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordGrid.SelectedItem is not ExamQuestionBankRecord selected)
            {
                MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"确认删除题目吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                _service.Delete(selected.Id);
                LoadRecords();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除题库失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRecords();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnFilterChanged(object sender, RoutedEventArgs e)
        {
            _recordView.Refresh();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            KeywordFilterText.Text = string.Empty;
            QuestionTypeFilterCombo.Text = string.Empty;
            DifficultyFilterCombo.Text = string.Empty;
            EnabledOnlyCheck.IsChecked = false;
            _recordView.Refresh();
        }

        private void ApplyButtonPermissions()
        {
            try
            {
                PagePermissionHelper.ApplyButtonPermissions(this, "exam_question_bank", _currentRole, _db);
            }
            catch
            {
                // ignore
            }
        }

        public void RefreshPermissionState()
        {
            ApplyButtonPermissions();
        }
    }
}
