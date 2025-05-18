using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Collections.Generic;

namespace FileAnalysisService.Data
{
    public enum AnalysisStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }

    public class AnalysisResult
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid FileId { get; set; }

        public int ParagraphCount { get; set; }
        public int WordCount { get; set; }
        public int SymbolCount { get; set; }

        public string WordCloudJson { get; set; }

        public AnalysisStatus Status { get; set; }
        public DateTime AnalysisTimestamp { get; set; }

        public string ErrorMessage { get; set; }
    }
}