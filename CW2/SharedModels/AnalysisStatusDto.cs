// SharedModels/AnalysisStatusDto.cs
using System;

namespace SharedModels
{
    public class AnalysisStatusDto
    {
        public Guid FileId { get; set; }
        public string Status { get; set; } // Например, "Completed", "Pending", "Failed"
    }
}
