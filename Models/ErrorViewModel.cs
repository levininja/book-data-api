using System;

namespace book_data_api.Models
{
    public class ErrorViewModel
    {
        public required string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
} 