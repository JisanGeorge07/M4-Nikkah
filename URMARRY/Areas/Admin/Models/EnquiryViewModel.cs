using Application.Models;
using Application.Models.Framework;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace URMARRY.Areas.Admin.Models;

public class EnquiryViewModel
{
    public EmailDto? Email { get; set; }
    public RegistrationDto? Enquiry { get; set; }
    public List<RegistrationDto>? Enquiries { get; set; }
    public List<ProfileForDto>? ProfileFor { get; set; }
    public List<NationalityDto>? Nationalities { get; set; }
	public List<StateDto>? States { get; set; }
	public List<DistrictDto>? Districts { get; set; }
	public List<CityDto>? Cities { get; set; }
	public List<MaritalStatusDto>? MaritalStatuses { get; set; }
    public List<BodyFeaturesDto>? BodyFeatures { get; set; }
    public List<ProfessionDto>? Professions { get; set; }
    public List<MotherTongueDto>? MotherTongues { get; set; }
    public List<ReligionCasteDto>? ReligionCastes { get; set; }
    public List<ReligiousnessDto>? Religiousnesses { get; set; }
    public List<CommunityDto>? Communities { get; set; }
    public List<FinancialStatusDto>? FinancialStatuses { get; set; }
    public HomeContentDto? HomeContent { get; set; }
    public ContactDto? Contact { get; set; }
    public string SelectedStatus { get; set; }
    public string NameFilter {  get; set; }
    public List<SelectListItem> Statuses { get; set; }
}