using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Constants;
using Application.Interfaces.Identity;
using Application.Models.Identity;
using Application.Models.Identity.Authentication;
using Application.Models.Identity.ChangePassword;
using Application.Models.Identity.Registration;
using AutoMapper;
using Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Services;

public class AccountService : IAccountService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IMapper _mapper;
    private readonly RoleManager<IdentityRole<long>> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtSettings,
        SignInManager<ApplicationUser> signInManager,
        IMapper mapper,
        RoleManager<IdentityRole<long>> roleManager
    )
    {
        _userManager = userManager;
        _jwtSettings = jwtSettings.Value;
        _signInManager = signInManager;
        _mapper = mapper;
        _roleManager = roleManager;
    }

    public async Task<AuthenticationResponse> Login(AuthenticationRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.UserName ??
                                                      throw new Exception("Username cannot be null."));

        if (user == null) throw new Exception("Invalid credentials.");

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName ?? string.Empty,
            request.Password ?? string.Empty,
            false,
            false);

        if (!result.Succeeded) throw new Exception("Invalid credentials.");

        var response = _mapper.Map<AuthenticationResponse>(user);
        response.Token = new JwtSecurityTokenHandler().WriteToken(GenerateToken(user).Result);

        response.Permissions = await GetUserPermissions(user);

        await _userManager.UpdateSecurityStampAsync(user);

        return response;
    }

    public async Task<RegistrationResponse> Register(ApplicationUserDto userDto)
    {
        var existingUser =
            await _userManager.FindByNameAsync(userDto.UserName ?? throw new Exception("Username cannot be null."));
        if (existingUser != null) throw new Exception($"Username '{userDto.UserName}' already exists.");

        var existingEmail =
            await _userManager.FindByEmailAsync(userDto.Email ?? throw new Exception("Email cannot be null."));
        if (existingEmail != null) throw new Exception($"Email {userDto.Email} already exists.");

        var user = new ApplicationUser
        {
            Email = userDto.Email,
            //FirstName = request.FirstName,
            //LastName = request.LastName,
            UserName = userDto.UserName,
            EmailConfirmed = true
        };

        var result =
            await _userManager.CreateAsync(user, userDto.Password ?? throw new Exception("Password cannot be null."));

        if (result.Succeeded)
        {
            foreach (var role in userDto.Roles!) await _userManager.AddToRoleAsync(user, role);
            return new RegistrationResponse { UserId = user.Id };
        }

        throw new Exception($"{result.Errors}");
    }

    public async Task<AuthenticationResponse?> GetCurrentUser(ClaimsPrincipal claimsPrincipal)
    {
        var email = claimsPrincipal.FindFirstValue(ClaimTypes.Email);
        var user = await _userManager.Users.SingleOrDefaultAsync(x => x.Email == email);
        await _signInManager.RefreshSignInAsync(user!);

        var userDto = _mapper.Map<AuthenticationResponse>(user);
        userDto.Token = new JwtSecurityTokenHandler().WriteToken(GenerateToken(user!).Result);
        userDto.Permissions = claimsPrincipal.FindAll(x => x.Type == CustomClaimTypes.Permission).Select(x => x.Value)
            .ToList();
        return userDto;
    }

    public async Task<ChangePasswordResponse> ChangePassword(ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId ?? throw new Exception("User ID cannot be null."));
        var result = await _userManager.ChangePasswordAsync(
            user ?? throw new Exception("User with the given ID not found."),
            request.CurrentPassword ?? throw new Exception("Password cannot be null."),
            request.NewPassword ?? throw new Exception("New password cannot be null."));

        return new ChangePasswordResponse
        {
            Succeeded = result.Succeeded,
            Errors = result.Errors.Select(x => x.Description).ToList()
        };
    }

    private async Task<List<string>> GetUserPermissions(ApplicationUser user)
    {
        var allPermissions = new List<string>();
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var permissions = (await _roleManager.GetClaimsAsync(role))
                .Where(x => x.Type == CustomClaimTypes.Permission).Select(x => x.Value);
            allPermissions.AddRange(permissions);
        }

        return allPermissions;
    }

    private async Task<JwtSecurityToken> GenerateToken(ApplicationUser applicationUser)
    {
        var userClaims = await _userManager.GetClaimsAsync(applicationUser);
        var roles = await _userManager.GetRolesAsync(applicationUser);

        var roleClaims = roles.Select(t => new Claim(ClaimTypes.Role, t)).ToList();

        foreach (var role in roles)
        {
            var identityRole = _roleManager.FindByNameAsync(role).Result;
            if (identityRole == null) continue;

            var permissions = _roleManager.GetClaimsAsync(identityRole).Result
                .Where(x => x.Type == CustomClaimTypes.Permission);
            roleClaims.AddRange(permissions);
        }

        var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, applicationUser.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, applicationUser.Email ?? string.Empty),
                new Claim(CustomClaimTypes.Uid, applicationUser.Id.ToString())
            }
            .Union(userClaims)
            .Union(roleClaims);

        var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key!));
        var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

        var jwtSecurityToken = new JwtSecurityToken(
            _jwtSettings.Issuer,
            _jwtSettings.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
            signingCredentials: signingCredentials);
        return jwtSecurityToken;
    }


    #region Admin Functions

    public List<ApplicationUserDto> GetAllUsers()
    {
        return _userManager.Users.ToList().Select(x => new ApplicationUserDto
        {
            Id = x.Id,
            UserName = x.UserName,
            Email = x.Email,
            Roles = _userManager.GetRolesAsync(x).Result.ToList()
        }).ToList();
    }

    public async Task<ApplicationUserDto> GetUser(long id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) throw new Exception("User with the given ID not found.");
        return new ApplicationUserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = _userManager.GetRolesAsync(user).Result.ToList()
        };
    }

    public async Task<long> UpdateUser(ApplicationUserDto userDto)
    {
        if (userDto.Id is not > 1) return Register(userDto).Id;

        var user = await _userManager.FindByIdAsync(userDto.Id.Value.ToString());
        if (user is null) throw new Exception("User with the given ID not found.");

        user.Email = userDto.Email;
        var hasher = new PasswordHasher<ApplicationUser>();
        if (!string.IsNullOrEmpty(userDto.Password))
            user.PasswordHash = hasher.HashPassword(user, userDto.Password);
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles) await _userManager.RemoveFromRoleAsync(user, role);

        foreach (var role in userDto.Roles!) await _userManager.AddToRoleAsync(user, role);

        return user.Id;
    }

    public async Task<ApplicationUserDto> DeleteUser(long id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) throw new Exception("User with the given ID not found.");

        await _userManager.DeleteAsync(user);

        return new ApplicationUserDto
        {
            UserName = user.UserName,
            Email = user.Email,
            Id = user.Id
        };
    }

    #endregion
}