namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 考试题库记录（exam_question_bank）
    /// </summary>
    public class ExamQuestionBankRecord
    {
        public long Id { get; set; }
        public string QuestionType { get; set; } = string.Empty;
        public string QuestionContent { get; set; } = string.Empty;
        public string OptionsJson { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Analysis { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string Remark { get; set; } = string.Empty;
    }
}
