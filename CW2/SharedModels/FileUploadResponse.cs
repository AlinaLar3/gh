// SharedModels/FileUploadResponse.cs
using System;

namespace SharedModels
{
    public class FileUploadResponse
    {
        public Guid FileId { get; set; }
        public string Status { get; set; } // e.g., "Uploaded", "DuplicateFound"
    }
}
