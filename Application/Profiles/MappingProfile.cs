using Application.Models;
using Application.Models.Common;
using Application.Models.Framework;
using AutoMapper;
using Domain;
using Domain.Common;
using Domain.Framework;

namespace Application.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<BaseEntity, BaseDto>()
            .IncludeAllDerived()
            .ReverseMap()
            .ForPath(x => x.Id, x => x.Ignore())
            .ForPath(x => x.CreatedOn, x => x.Ignore())
            .ForPath(x => x.ModifiedOn, x => x.Ignore())
            .ForPath(x => x.ModifiedOn, x => x.Ignore())
            .ForPath(x => x.ModifiedBy, x => x.Ignore())
            .ForPath(x => x.IsDeleted, x => x.Ignore());

        CreateMap<OrderableBaseEntity, OrderableDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        CreateMap<int?, int>().ConvertUsing((src, dest) => src ?? dest);

        //Page
        CreateMap<PageSettings, PageSettingsDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //SocialMedia
        CreateMap<SocialMedia, SocialMediaDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //Email
        CreateMap<Email, EmailDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //HomeContent
        CreateMap<HomeContent, HomeContentDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //ImageGallery
        CreateMap<ImageGallery, ImageGalleryDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //ProfileFor
        CreateMap<ProfileFor, ProfileForDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //Nationality
        CreateMap<Nationality, NationalityDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();
        
        //State
        CreateMap<State, StateDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        CreateMap<District, DistrictDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        CreateMap<City, CityDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //MaritalStatus
        CreateMap<MaritalStatus, MaritalStatusDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //BodyFeatures
        CreateMap<BodyFeatures, BodyFeaturesDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //Profession
        CreateMap<Profession, ProfessionDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //MotherTongue
        CreateMap<MotherTongue, MotherTongueDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //ReligionCaste
        CreateMap<ReligionCaste, ReligionCasteDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //Community
        CreateMap<Community, CommunityDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //Religiousness
        CreateMap<Religiousness, ReligiousnessDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();
        
        //FinancialStatus
        CreateMap<FinancialStatus, FinancialStatusDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //Registration
        CreateMap<Registration, RegistrationDto>()
            .IncludeBase<BaseEntity, BaseDto>()
            .AfterMap((src, dest) =>
            {
                if (!string.IsNullOrEmpty(src.DOB))
                {
                    var parts = src.DOB.Split('/');
                    if (parts.Length == 3)
                    {
                        dest.Day = parts[0];
                        dest.Month = parts[1];
                        dest.Year = parts[2];
                    }
                }
            })
            .ReverseMap();

        //About
        CreateMap<About, AboutDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //Contact
        CreateMap<Contact, ContactDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

		//HomeBanner
		CreateMap<HomeBanner, HomeBannerDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //MatchingProfiles
        CreateMap<MatchingProfiles, MatchingProfilesDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //Images
        CreateMap<Images, ImagesDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();
        
        //UserReport
        CreateMap<UserReport, UserReportDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //ContactEnquiry
        CreateMap<ContactEnquiry, ContactEnquiryDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();
        
        //SupportRequest
        CreateMap<SupportRequest, SupportRequestDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();
        
        CreateMap<UserContactView, UserContactViewDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        CreateMap<UserNotLikeProfile, UserNotLikeProfileDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        CreateMap<UserReportReason, UserReportReasonDto>().IncludeBase<OrderableBaseEntity, OrderableDto>().ReverseMap();

        //DeleteReason
        CreateMap<DeleteReason, DeleteReasonDto>().IncludeBase<OrderableBaseEntity, OrderableDto>().ReverseMap();

        //MatchStatusUpdate
        CreateMap<MatchStatusUpdate, MatchStatusUpdateDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //SuccessStory
        CreateMap<SuccessStory, SuccessStoryDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //TestimonialSetting
        CreateMap<TestimonialSetting, TestimonialSettingDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //ColdLead
        CreateMap<ColdLead, ColdLeadDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();

        //VerificationDocument
        CreateMap<VerificationDocument, VerificationDocumentDto>().IncludeBase<BaseEntity, BaseDto>().ReverseMap();
    }
}