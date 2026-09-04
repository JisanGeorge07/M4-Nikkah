using Application.Models.Common;
using Microsoft.AspNetCore.Http;

namespace Application.Models
{
    public class RegistrationDto : BaseDto
    {
        public long ProfileForId { get; set; }
        public string? ProfileFor { get; set; }
        public string? Name { get; set; }
        public string? Gender { get; set; }
        public string? DOB { get; set; }
        public string? Day { get; set; }
        public string? Month { get; set; }
        public string? Year { get; set; }
        public long NationalityId { get; set; }
        public string? Nationality { get; set; }
        public string? CountryCode { get; set; }
        public string? Phone { get; set; }
        public long MaritalStatusId { get; set; }
		public long? NumberOfChildrens { get; set; }
		public string? MaritalStatus { get; set; }
        public long HeightId { get; set; }
        public string? Height { get; set; }
        public long WeightId { get; set; }
        public string? Weight { get; set; }
        public long ComplexionId { get; set; }
        public string? Complexion { get; set; }
        public long BodyTypeId { get; set; }
        public string? BodyType { get; set; }
        public bool IsPhysicallyChallenged { get; set; }
        public string? PhysicallyChallengedDetail { get; set; }
        public string? HighestEducation { get; set; }
        public string? EducationType { get; set; }
        public long ProfessionId { get; set; }
        public string? Profession { get; set; }
        public string? ProfessionType { get; set; }
        public long MotherTongueId { get; set; }
        public string? MotherTongue { get; set; }
        public long ReligionId { get; set; }
        public string? Religion { get; set; }
        public long CasteId { get; set; }
        public string? Caste { get; set; }
        public long CommunityId { get; set; }
        public string? Community { get; set; }
        public long ReligiousnessId { get; set; }
        public string? Religiousness { get; set; }
        public long FinancialStatusId { get; set; }
        public string? FinancialStatus { get; set; }
        public string? FamilyName { get; set; }
        public string? FatherName { get; set; }
        public string? Post { get; set; }
        public string? Village { get; set; }
        public string? PinCode { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? States { get; set; }
        public string? District { get; set; }
		public string? LandlineNumber { get; set; }
		public string? SecondaryCountryCode { get; set; }
		public string? PresentCountry { get; set; }
		public string? PresentCountryCode { get; set; }
        public string? PresentState { get; set; }
        public string? PresentDistrict { get; set; }
        public string? PresentCity { get; set; }
        public string? ImagePath { get; set; }
        public IFormFile? Image { get; set; }
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
        public bool IsLiked { get; set; }
        public bool IsStarred { get; set; }
        public int total_matching_score { get; set; } = 0;
        public string? Status { get; set; }
        public short DisabledReason { get; set; } = Constants.DisabledReason.Deactivated;
        public bool IsVisible { get; set; } = false;
        public bool PhotoVisibleToAll { get; set; } = true;
        public bool PhotoVisibleToPremium { get; set; } = false;
        public bool PhotoVisibleToAccepted { get; set; } = false;
        public bool PhotosUnlocked { get; set; } = false;
        public int PhotoUnlockRequestStatus { get; set; } = -1;
        public DateTime? OtpGeneratedAt { get; set; }
        public int OtpResendCount { get; set; } = 0;
        public bool? StaffCreated { get; set; }
        public long? StaffId { get; set; }
        public long? DeleteReasonId { get; set; }
        public string? DeleteReasonText { get; set; }
        public bool IsDeleted { get; set; }
        public bool DocumentVerificationEnabled { get; set; }
        public string? VerificationDocumentUrl { get; set; }
        public bool DocumentVerificationComplete { get; set; }
        public bool DocumentVerificationRejected { get; set; }
        public bool DocumentVerificationFollowupApproved { get; set; }
        public List<VerificationDocumentDto> VerificationDocuments { get; set; } = new List<VerificationDocumentDto>();
        public DateTime? LastSeenAt { get; set; }
    }
}
