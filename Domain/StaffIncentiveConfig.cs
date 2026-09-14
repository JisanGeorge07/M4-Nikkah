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

        // 2. Female Grade A Profile Verification
        public string? FemaleGradeAVerificationType { get; set; } // "TargetBasis" or "ProfileBasis"
        public int? FemaleGradeAVerificationTarget { get; set; }
        public decimal FemaleGradeAVerificationAmount { get; set; }

        // 3. Female Grade B Profile Verification
        public string? FemaleGradeBVerificationType { get; set; } // "TargetBasis" or "ProfileBasis"
        public int? FemaleGradeBVerificationTarget { get; set; }
        public decimal FemaleGradeBVerificationAmount { get; set; }

        // 4. Female Grade C Profile Verification
        public string? FemaleGradeCVerificationType { get; set; } // "TargetBasis" or "ProfileBasis"
        public int? FemaleGradeCVerificationTarget { get; set; }
        public decimal FemaleGradeCVerificationAmount { get; set; }

        // 5. Female Grade D Profile Verification
        public string? FemaleGradeDVerificationType { get; set; } // "TargetBasis" or "ProfileBasis"
        public int? FemaleGradeDVerificationTarget { get; set; }
        public decimal FemaleGradeDVerificationAmount { get; set; }

        // Legacy Female Normal Profile Verification (Kept for DB compatibility)
        public string? FemaleVerificationType { get; set; } // "TargetBasis" or "ProfileBasis"
        public int? FemaleVerificationTarget { get; set; }
        public decimal FemaleVerificationAmount { get; set; }

        // Legacy Female Document Profile Verification (Kept for DB compatibility)
        public string? FemaleDocVerificationType { get; set; } // "TargetBasis" or "ProfileBasis"
        public int? FemaleDocVerificationTarget { get; set; }
        public decimal FemaleDocVerificationAmount { get; set; }

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
