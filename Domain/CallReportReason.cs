using System;
using Domain.Common;

namespace Domain;

public class CallReportReason : OrderableBaseEntity
{
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconName { get; set; }
}
