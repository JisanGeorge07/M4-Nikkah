using Application.Models.Common;
using Domain;
namespace Application.Models;

public class UserReportDto: BaseDto
{
    public long ReporterUserId { get; set; } = 0;
    public long ReportedUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    
    public UserReportStatus Status { get; set; }
    public string? ModeratorNotes { get; set; }
    public string? ActionedBy { get; set; }
    public string? ActionedByUsername { get; set; }
    public DateTime? ActionedOn { get; set; }
}
