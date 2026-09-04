using Application.Models.Framework;
using MimeKit;

namespace Application.Interfaces.Infrastructure.Email;

public class Message
{
    public List<MailboxAddress>? To { get; set; }
    public EmailDto? From { get; set; }
    public string? Subject { get; set; }
    public string? Content { get; set; }
    public List<string?> AttachmentPaths { get; set; } = new();
}