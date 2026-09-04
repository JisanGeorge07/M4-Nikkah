using Domain.Common;
using System;

namespace Domain
{
    public class StaffDetail : OrderableBaseEntity
    {
        public long UserId { get; set; }
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public DateTime? JoiningDate { get; set; }
    }
}
