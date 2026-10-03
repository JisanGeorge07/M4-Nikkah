using Application.Models.Call;
using Domain;
using System;
using System.Collections.Generic;

namespace URMARRY.Areas.Admin.Models
{
    public class CallReportListViewModel
    {
        public List<CallReportDto> Reports { get; set; } = new List<CallReportDto>();
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ActionedCount { get; set; }
        public int DismissedCount { get; set; }
        public string? SelectedStatus { get; set; }
    }

    public class CallReportDetailViewModel
    {
        public CallReportDto Report { get; set; } = new CallReportDto();
        public Registration? Reporter { get; set; }
        public Registration? ReportedUser { get; set; }
        public CallLog? CallLog { get; set; }
        public bool BackupFileExists { get; set; }
        public string? PlaybackUrl { get; set; }
    }
}
