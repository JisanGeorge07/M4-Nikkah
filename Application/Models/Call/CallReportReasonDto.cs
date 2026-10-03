using System;
using Application.Models.Common;

namespace Application.Models.Call
{
    public class CallReportReasonDto : OrderableDto
    {
        public string Reason { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconName { get; set; }
    }
}
