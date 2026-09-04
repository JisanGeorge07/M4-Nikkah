using Application.Models.Identity.Authentication;
using AutoMapper;
using Identity.Models;

namespace Identity.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ApplicationUser, AuthenticationResponse>().ReverseMap();
    }
}