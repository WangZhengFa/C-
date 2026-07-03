using System;

namespace FoodEnterpriseIMS.Models
{
    /// <summary>
    /// 编制文件记录（document_files）
    /// </summary>
    public class DocumentFileRecord
    {
        public long Id { get; set; }
        public string NodeCode { get; set; } = string.Empty;
        public string FileUniqueId { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string StdCategory { get; set; } = string.Empty;
        public string StdLevel1 { get; set; } = string.Empty;
        public string StdLevel2 { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileCode { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Revision { get; set; } = string.Empty;
        public DateTime? RevisionDate { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string FileLink { get; set; } = string.Empty;
        public bool IsInvalid { get; set; }
        public string Remark { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
