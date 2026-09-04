using System;
using Domain.Common;

namespace Domain;

public class UserReportReason:OrderableBaseEntity
{
    public string? Reason { get; set; }
    public string? IconName { get; set; }
    public bool ShowDescriptionPopup { get; set; }
}
