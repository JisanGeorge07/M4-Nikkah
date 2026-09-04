using Domain.Common;
using System;

namespace Domain
{
    public class StaffIncentiveConfig : BaseEntity
    {
        public long StaffId { get; set; }

        // 1. Male Profile Verification
        public string? MaleVerificationType { get; set; } // "TargetBasis" or "ProfileBasis"
        public int? MaleVerificationTarget { get; set; }
        public decimal MaleVerificationAmount { get; set; }

        // 2. Female Profile Verification
        public string? FemaleVerificationType { get; set; } // "TargetBasis" or "ProfileBasis"
        public int? FemaleVerificationTarget { get; set; }
        public decimal FemaleVerificationAmount { get; set; }

        // 3. Male Premium Conversion
        public string? MaleConversionType { get; set; } // "TargetBasis" or "ProfileBasis"
        public int? MaleConversionTarget { get; set; }
        public decimal MaleConversionAmount { get; set; }

        // 4. Female Premium Conversion
        public string? FemaleConversionType { get; set; } // "TargetBasis" or "ProfileBasis"
        public int? FemaleConversionTarget { get; set; }
        public decimal FemaleConversionAmount { get; set; }
    }
}
