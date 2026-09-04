using Domain.Common;
using System;

namespace Domain
{
    public class StaffPerformanceTargetConfig : BaseEntity
    {
        public long StaffId { get; set; }

        // 1. Male Profile Verification Targets
        public int MaleVerificationMonthlyTarget { get; set; }
        public int MaleVerificationDailyTarget { get; set; }

        // 2. Female Profile Verification Targets
        public int FemaleVerificationMonthlyTarget { get; set; }
        public int FemaleVerificationDailyTarget { get; set; }

        // 3. Male Premium Conversion Targets
        public int MaleConversionMonthlyTarget { get; set; }
        public int MaleConversionDailyTarget { get; set; }

        // 4. Female Premium Conversion Targets
        public int FemaleConversionMonthlyTarget { get; set; }
        public int FemaleConversionDailyTarget { get; set; }
    }
}
