using System;

namespace book_data_api.Models
{
    public class ErrorLog
    {
        public int Id { get; set; }
        public string LogLevel { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public DateTime LogDate { get; set; } = DateTime.UtcNow;
    }
} 