using Application.Models.Common;

namespace Application.Models.Framework;

public class EmailDto : BaseDto
{
    public string? SmtpServer { get; set; }
    public int Port { get; set; }

    public string? Name { get; set; }
    public string? EmailId { get; set; }
    public string? Password { get; set; }
    public string? Purpose { get; set; }

    public string? Recipients { get; set; }
}