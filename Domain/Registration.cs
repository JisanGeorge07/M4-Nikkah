using Domain.Common;

namespace Domain
{
    public class Registration : BaseEntity
    {
        public long ProfileForId { get; set; }
        public string? Name { get; set; }
        public string? Gender { get; set; }
        public string? DOB { get; set; }
        public long NationalityId { get; set; }
        public string? CountryCode { get; set; }
        public string? Phone { get; set; }
        public long MaritalStatusId { get; set; }
        public long? NumberOfChildrens { get; set; }
        public long HeightId { get; set; }
        public long WeightId { get; set; }
        public long ComplexionId { get; set; }
        public long BodyTypeId { get; set; }
        public bool IsPhysicallyChallenged { get; set; }
        public string? PhysicallyChallengedDetail { get; set; }
        public string? HighestEducation { get; set; }
        public string? EducationType { get; set; }
        public long ProfessionId { get; set; }
        public string? ProfessionType { get; set; }
        public long MotherTongueId { get; set; }
        public long ReligionId { get; set; }
        public long CasteId { get; set; }
        public long CommunityId { get; set; }
        public long ReligiousnessId { get; set; }
        public long FinancialStatusId { get; set; }
        public string? FamilyName { get; set; }
        public string? FatherName { get; set; }
        public string? Post { get; set; }
        public string? Village { get; set; }
        public string? PinCode { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? District { get; set; }
        public string? LandlineNumber { get; set; }
        public string? SecondaryCountryCode { get; set; }
        public string? PresentCountry { get; set; }
        public string? PresentState { get; set; }
        public string? PresentDistrict { get; set; }
        public string? PresentCity { get; set; }
        public string? ImagePath { get; set; }
        public string? About { get; set; }
        public string? RegisterNumber { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? PasswordHash { get; set; }
        public string? VerificationCode { get; set; }
        public bool IsVerified { get; set; }
        public bool IsComplete { get; set; }
        public bool IsPremiumMember { get; set; }
        public bool ShowOnHomePage { get; set; }
        public string? CompletedStep { get; set; }
        public string? Source { get; set; }
        public bool IsSpecialRequest { get; set; }
        public short DisabledReason { get; set; }
        public bool IsVisible { get; set; } = false;
        public bool PhotoVisibleToAll { get; set; } = true;
        public bool PhotoVisibleToPremium { get; set; } = false;
        public bool PhotoVisibleToAccepted { get; set; } = false;
        public DateTime? OtpGeneratedAt { get; set; }
        public int OtpResendCount { get; set; } = 0;
        public bool? StaffCreated { get; set; }
        public long? StaffId { get; set; }
        public long? DeleteReasonId { get; set; }
        public string? DeleteReasonText { get; set; }
        public virtual DeleteReason? DeleteReason { get; set; }
        public bool DocumentVerificationEnabled { get; set; } = false;
        public string? VerificationDocumentUrl { get; set; }
        public bool DocumentVerificationComplete { get; set; } = false;
        public bool DocumentVerificationRejected { get; set; } = false;
        public bool DocumentVerificationFollowupApproved { get; set; } = false;
        public DateTime? LastSeenAt { get; set; }
        public virtual ICollection<VerificationDocument> VerificationDocuments { get; set; } = new List<VerificationDocument>();
    }
}
