using System.Security.Claims;
using Application.Models.Identity;
using Application.Models.Identity.Authentication;
using Application.Models.Identity.ChangePassword;
using Application.Models.Identity.Registration;

namespace Application.Interfaces.Identity;

public interface IAccountService
{
    Task<AuthenticationResponse> Login(AuthenticationRequest request);
    Task<RegistrationResponse> Register(ApplicationUserDto userDto);

    Task<AuthenticationResponse?> GetCurrentUser(ClaimsPrincipal claimsPrincipal);

    Task<ChangePasswordResponse> ChangePassword(ChangePasswordRequest request);

    List<ApplicationUserDto> GetAllUsers();
    Task<ApplicationUserDto> GetUser(long id);
    Task<long> UpdateUser(ApplicationUserDto userDto);
    Task<ApplicationUserDto> DeleteUser(long id);
}