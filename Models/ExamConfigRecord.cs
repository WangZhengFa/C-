using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 考试配置记录（exam_config）
    /// </summary>
    public class ExamConfigRecord
    {
        public long Id { get; set; }
        public string ConfigId { get; set; } = string.Empty;
        public string NodeCode { get; set; } = string.Empty;
        public string ExamName { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public int PassScore { get; set; }
        public int DurationMinutes { get; set; }
        public int JudgeCount { get; set; }
        public decimal JudgeSingleScore { get; set; }
        public int SingleCount { get; set; }
        public decimal SingleSingleScore { get; set; }
        public int MultiCount { get; set; }
        public decimal MultiSingleScore { get; set; }
        public int EssayCount { get; set; }
        public decimal EssaySingleScore { get; set; }
        public string CategoryFilter { get; set; } = string.Empty;
        public string DifficultyFilter { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string Remark { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
