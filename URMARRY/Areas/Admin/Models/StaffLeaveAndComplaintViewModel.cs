using System;
using System.Collections.Generic;

namespace URMARRY.Areas.Admin.Models
{
    public class StaffLeaveAndComplaintViewModel
    {
        public long SelectedStaffId { get; set; }
        public int FilterYear { get; set; } = DateTime.UtcNow.Year;
        public int FilterMonth { get; set; } = DateTime.UtcNow.Month;

        public List<StaffSelectOption> StaffOptions { get; set; } = new();

        // Selected Staff Info
        public string StaffName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal BasicMonthlySalary { get; set; }
        public int DaysInMonth { get; set; } = 30;
        public decimal PerDaySalary { get; set; }
        public int PaidLeaveLimit { get; set; } = 0;

        // Records
        public List<StaffLeaveRecordItem> LeaveRecords { get; set; } = new();
        public List<StaffComplaintRecordItem> ComplaintRecords { get; set; } = new();

        // Summaries
        public decimal TotalLeaveDeduction { get; set; }
        public decimal TotalComplaintDeduction { get; set; }
        public decimal TotalDeductions => TotalLeaveDeduction + TotalComplaintDeduction;
    }

    public class StaffSelectOption
    {
        public long Id { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }

    public class StaffLeaveRecordItem
    {
        public long Id { get; set; }
        public long StaffId { get; set; }
        public DateTime LeaveDate { get; set; }
        public string LeaveType { get; set; } = "FullDay";
        public string? Reason { get; set; }
        public bool IsPaid { get; set; }
        public bool IsApproved { get; set; }
        public string ApprovedBy { get; set; } = "Admin";
        public decimal CalculatedDeduction { get; set; }
    }

    public class StaffComplaintRecordItem
    {
        public long Id { get; set; }
        public long StaffId { get; set; }
        public DateTime ComplaintDate { get; set; }
        public string? ComplaintDescription { get; set; }
        public string? ComplaintLevel { get; set; }
        public decimal DeductionAmount { get; set; }
        public bool IsApproved { get; set; }
        public string ApprovedBy { get; set; } = "Admin";
        public string? Resolution { get; set; }
    }
}
