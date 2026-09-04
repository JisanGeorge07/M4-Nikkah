using System;
using Application.Models.Common;

namespace Application.Models;

public class UserReportReasonDto : OrderableDto
{
    public string? Reason { get; set; }
    public string? IconName { get; set; }
    public bool ShowDescriptionPopup { get; set; }
}
