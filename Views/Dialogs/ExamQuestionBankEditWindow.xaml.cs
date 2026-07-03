using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 考试题库编辑窗口
    /// </summary>
    public partial class ExamQuestionBankEditWindow : Window
    {
        public ExamQuestionBankRecord Value { get; private set; }

        public ExamQuestionBankEditWindow(ExamQuestionBankRecord? source, IEnumerable<ExamQuestionBankRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new ExamQuestionBankRecord { IsEnabled = true, Difficulty = "中等", QuestionType = "单选题" } : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<ExamQuestionBankRecord> existing)
        {
            QuestionTypeCombo.Items.Clear();
            QuestionTypeCombo.Items.Add("单选题");
            QuestionTypeCombo.Items.Add("多选题");
            QuestionTypeCombo.Items.Add("判断题");

            DifficultyCombo.Items.Clear();
            DifficultyCombo.Items.Add("简单");
            DifficultyCombo.Items.Add("中等");
            DifficultyCombo.Items.Add("困难");

            foreach (var value in existing.Select(x => x.QuestionType).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!QuestionTypeCombo.Items.Contains(value))
                {
                    QuestionTypeCombo.Items.Add(value);
                }
            }

            foreach (var value in existing.Select(x => x.Difficulty).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!DifficultyCombo.Items.Contains(value))
                {
                    DifficultyCombo.Items.Add(value);
                }
            }
        }

        private void BindValue()
        {
            QuestionTypeCombo.Text = string.IsNullOrWhiteSpace(Value.QuestionType) ? "单选题" : Value.QuestionType;
            CategoryText.Text = Value.Category;
            QuestionContentText.Text = Value.QuestionContent;
            OptionsJsonText.Text = Value.OptionsJson;
            AnswerText.Text = Value.Answer;
            AnalysisText.Text = Value.Analysis;
            DifficultyCombo.Text = string.IsNullOrWhiteSpace(Value.Difficulty) ? "中等" : Value.Difficulty;
            IsEnabledCheck.IsChecked = Value.IsEnabled;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var questionContent = QuestionContentText.Text.Trim();
            var answer = AnswerText.Text.Trim();
            if (string.IsNullOrWhiteSpace(questionContent))
            {
                MessageBox.Show(this, "题目内容不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(answer))
            {
                MessageBox.Show(this, "答案不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.QuestionType = QuestionTypeCombo.Text.Trim();
            Value.Category = CategoryText.Text.Trim();
            Value.QuestionContent = questionContent;
            Value.OptionsJson = OptionsJsonText.Text.Trim();
            Value.Answer = answer;
            Value.Analysis = AnalysisText.Text.Trim();
            Value.Difficulty = DifficultyCombo.Text.Trim();
            Value.IsEnabled = IsEnabledCheck.IsChecked == true;
            Value.Remark = RemarkText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static ExamQuestionBankRecord Clone(ExamQuestionBankRecord source)
        {
            return new ExamQuestionBankRecord
            {
                Id = source.Id,
                QuestionType = source.QuestionType,
                QuestionContent = source.QuestionContent,
                OptionsJson = source.OptionsJson,
                Answer = source.Answer,
                Analysis = source.Analysis,
                Category = source.Category,
                Difficulty = source.Difficulty,
                IsEnabled = source.IsEnabled,
                Remark = source.Remark
            };
        }
    }
}
