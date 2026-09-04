using Application.Models;

namespace URMARRY.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public List<PageSettingsDto>? Pages { get; set; }
        public string? ErrorCode { get; set; }
        public string? Message { get; set; }
    }
}