using Microsoft.AspNetCore.Http;

namespace Application.Models
{
    public class SupportAppealSubmitModel
    {
        public long UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public IFormFile? Attachment { get; set; }
    }
}
