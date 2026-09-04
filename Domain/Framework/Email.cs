using Domain.Common;

namespace Domain.Framework;

public class Email : BaseEntity
{
    public string? SmtpServer { get; set; }
    public int Port { get; set; }
    public string? Name { get; set; }
    public string? EmailId { get; set; }
    public string? Password { get; set; }
    public string? Purpose { get; set; }

    public string? Recipients { get; set; }
}