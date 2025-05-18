// SharedModels/AnalysisResultDto.cs
using System;
using System.Collections.Generic;

namespace SharedModels
{
    public class AnalysisResultDto
    {
        public Guid FileId { get; set; }
        public int ParagraphCount { get; set; }
        public int WordCount { get; set; }
        public int SymbolCount { get; set; }
        public Dictionary<string, int> WordCloudData { get; set; } // Пример: слово -> частота
        public string Status { get; set; } // Например, "Completed", "Pending", "Failed"
        public string ErrorMessage { get; set; } // Если статус "Failed"
    }
}
