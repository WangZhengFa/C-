using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using FoodEnterpriseIMS.Models;

namespace 食品信息管理系统.Views.Dialogs
{
    /// <summary>
    /// 考试配置编辑窗口
    /// </summary>
    public partial class ExamConfigEditWindow : Window
    {
        public ExamConfigRecord Value { get; private set; }

        public ExamConfigEditWindow(ExamConfigRecord? source, IEnumerable<ExamConfigRecord> existing)
        {
            InitializeComponent();
            Value = source == null ? new ExamConfigRecord { IsEnabled = true, ExamType = "电脑考试", TotalScore = 100, PassScore = 60, DurationMinutes = 60 } : Clone(source);
            InitCombos(existing);
            BindValue();
        }

        private void InitCombos(IEnumerable<ExamConfigRecord> existing)
        {
            ExamTypeCombo.Items.Clear();
            ExamTypeCombo.Items.Add("电脑考试");
            ExamTypeCombo.Items.Add("笔试");
            ExamTypeCombo.Items.Add("在线考试");

            foreach (var value in existing.Select(x => x.ExamType).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
            {
                if (!ExamTypeCombo.Items.Contains(value))
                {
                    ExamTypeCombo.Items.Add(value);
                }
            }
        }

        private void BindValue()
        {
            ConfigIdText.Text = Value.ConfigId;
            NodeCodeText.Text = Value.NodeCode;
            ExamNameText.Text = Value.ExamName;
            ExamTypeCombo.Text = Value.ExamType;
            DepartmentText.Text = Value.Department;
            TotalScoreText.Text = Value.TotalScore.ToString(CultureInfo.InvariantCulture);
            PassScoreText.Text = Value.PassScore.ToString(CultureInfo.InvariantCulture);
            DurationMinutesText.Text = Value.DurationMinutes.ToString(CultureInfo.InvariantCulture);
            JudgeCountText.Text = Value.JudgeCount.ToString(CultureInfo.InvariantCulture);
            JudgeSingleScoreText.Text = Value.JudgeSingleScore.ToString(CultureInfo.InvariantCulture);
            SingleCountText.Text = Value.SingleCount.ToString(CultureInfo.InvariantCulture);
            SingleSingleScoreText.Text = Value.SingleSingleScore.ToString(CultureInfo.InvariantCulture);
            MultiCountText.Text = Value.MultiCount.ToString(CultureInfo.InvariantCulture);
            MultiSingleScoreText.Text = Value.MultiSingleScore.ToString(CultureInfo.InvariantCulture);
            EssayCountText.Text = Value.EssayCount.ToString(CultureInfo.InvariantCulture);
            EssaySingleScoreText.Text = Value.EssaySingleScore.ToString(CultureInfo.InvariantCulture);
            CategoryFilterText.Text = Value.CategoryFilter;
            DifficultyFilterText.Text = Value.DifficultyFilter;
            CreatedByText.Text = Value.CreatedBy;
            IsEnabledCheck.IsChecked = Value.IsEnabled;
            RemarkText.Text = Value.Remark;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var examName = ExamNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(examName))
            {
                MessageBox.Show(this, "考试名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(TotalScoreText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var totalScore))
            {
                MessageBox.Show(this, "总分必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(PassScoreText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var passScore))
            {
                MessageBox.Show(this, "及格分必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(DurationMinutesText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var durationMinutes))
            {
                MessageBox.Show(this, "时长必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(JudgeCountText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var judgeCount))
            {
                MessageBox.Show(this, "判断题数必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!decimal.TryParse(JudgeSingleScoreText.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var judgeSingleScore))
            {
                MessageBox.Show(this, "判断单分格式不正确", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(SingleCountText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var singleCount))
            {
                MessageBox.Show(this, "单选题数必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!decimal.TryParse(SingleSingleScoreText.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var singleSingleScore))
            {
                MessageBox.Show(this, "单选单分格式不正确", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(MultiCountText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var multiCount))
            {
                MessageBox.Show(this, "多选题数必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!decimal.TryParse(MultiSingleScoreText.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var multiSingleScore))
            {
                MessageBox.Show(this, "多选单分格式不正确", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(EssayCountText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var essayCount))
            {
                MessageBox.Show(this, "简答题数必须是整数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!decimal.TryParse(EssaySingleScoreText.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var essaySingleScore))
            {
                MessageBox.Show(this, "简答单分格式不正确", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Value.ConfigId = ConfigIdText.Text.Trim();
            Value.NodeCode = NodeCodeText.Text.Trim();
            Value.ExamName = examName;
            Value.ExamType = ExamTypeCombo.Text.Trim();
            Value.Department = DepartmentText.Text.Trim();
            Value.TotalScore = totalScore;
            Value.PassScore = passScore;
            Value.DurationMinutes = durationMinutes;
            Value.JudgeCount = judgeCount;
            Value.JudgeSingleScore = judgeSingleScore;
            Value.SingleCount = singleCount;
            Value.SingleSingleScore = singleSingleScore;
            Value.MultiCount = multiCount;
            Value.MultiSingleScore = multiSingleScore;
            Value.EssayCount = essayCount;
            Value.EssaySingleScore = essaySingleScore;
            Value.CategoryFilter = CategoryFilterText.Text.Trim();
            Value.DifficultyFilter = DifficultyFilterText.Text.Trim();
            Value.IsEnabled = IsEnabledCheck.IsChecked == true;
            Value.Remark = RemarkText.Text.Trim();
            Value.CreatedBy = CreatedByText.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static ExamConfigRecord Clone(ExamConfigRecord source)
        {
            return new ExamConfigRecord
            {
                Id = source.Id,
                ConfigId = source.ConfigId,
                NodeCode = source.NodeCode,
                ExamName = source.ExamName,
                ExamType = source.ExamType,
                Department = source.Department,
                TotalScore = source.TotalScore,
                PassScore = source.PassScore,
                DurationMinutes = source.DurationMinutes,
                JudgeCount = source.JudgeCount,
                JudgeSingleScore = source.JudgeSingleScore,
                SingleCount = source.SingleCount,
                SingleSingleScore = source.SingleSingleScore,
                MultiCount = source.MultiCount,
                MultiSingleScore = source.MultiSingleScore,
                EssayCount = source.EssayCount,
                EssaySingleScore = source.EssaySingleScore,
                CategoryFilter = source.CategoryFilter,
                DifficultyFilter = source.DifficultyFilter,
                IsEnabled = source.IsEnabled,
                Remark = source.Remark,
                CreatedBy = source.CreatedBy,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };
        }
    }
}
