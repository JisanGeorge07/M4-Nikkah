using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace URMARRY.Services;

/// <summary>
/// Provides secure JWT-in-cookie authentication utilities.
/// Replaces the insecure raw "id" cookie with a cryptographically signed JWT token
/// stored in a secure HttpOnly cookie.
/// </summary>
public class CookieHelper
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CookieHelper> _logger;

    // Cookie configuration constants
    private const string CookieName = "auth_session";
    private const int CookieExpiryDays = 7; // Original production value
    //private const int CookieExpiryMinutes = 5; // For testing

    public CookieHelper(IConfiguration configuration, ILogger<CookieHelper> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Generates a JWT token for the given user ID and email, then stores it
    /// in a secure HttpOnly cookie on the response.
    /// </summary>
    public void SetSecureJwtCookie(HttpContext httpContext, long userId, string? email = null)
    {
        var token = GenerateJwtToken(userId, email);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,           // Prevents JavaScript access (XSS protection)
            Secure = true,             // Only sent over HTTPS
            SameSite = SameSiteMode.Strict, // Prevents CSRF attacks
            Expires = DateTimeOffset.UtcNow.AddDays(CookieExpiryDays), // Original production value
            // Expires = DateTimeOffset.UtcNow.AddMinutes(CookieExpiryMinutes), // For testing
            IsEssential = true
        };

        httpContext.Response.Cookies.Append(CookieName, token, cookieOptions);
        _logger.LogInformation("Secure JWT cookie set for user ID: {UserId}", userId);
    }

    /// <summary>
    /// Reads the authenticated user's ID from the JWT cookie first,
    /// then falls back to the Authorization: Bearer header (for mobile apps).
    /// Returns null if no valid token is found in either source.
    /// </summary>
    public long? GetUserIdFromCookie(HttpContext httpContext)
    {
        string? token = null;

        // 1. Try the Authorization: Bearer header first (React Native / mobile flow takes priority)
        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = authHeader.Substring("Bearer ".Length).Trim();
        }

        // 2. Fall back to cookie (web browser flow)
        if (string.IsNullOrEmpty(token))
        {
            token = httpContext.Request.Cookies[CookieName];
        }

        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        try
        {
            var secret = _configuration["JwtSettings:Secret"];
            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogError("JWT secret is not configured in appsettings.json under JwtSettings:Secret");
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5) // Original production value
                //ClockSkew = TimeSpan.Zero // For testing
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var userIdClaim = principal.FindFirst(ClaimTypes.Name)?.Value;

            if (long.TryParse(userIdClaim, out long userId))
            {
                return userId;
            }

            _logger.LogWarning("JWT token contained invalid user ID claim: {Claim}", userIdClaim);
            return null;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogInformation("JWT token has expired, clearing cookie");
            ClearSecureCookie(httpContext);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate JWT token - possible tampering detected");
            ClearSecureCookie(httpContext);
            return null;
        }
    }

    /// <summary>
    /// Clears the secure JWT cookie (used during logout).
    /// </summary>
    public void ClearSecureCookie(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        });

        // Also clear the legacy "id" cookie if it still exists
        if (httpContext.Request.Cookies.ContainsKey("id"))
        {
            httpContext.Response.Cookies.Delete("id");
        }

        _logger.LogInformation("Secure JWT cookie cleared");
    }

    /// <summary>
    /// Generates a JWT token containing the user's ID and email as claims.
    /// Uses the same signing algorithm (HMAC-SHA256) as the API project.
    /// </summary>
    public string GenerateJwtToken(long userId, string? email)
    {
        var secret = _configuration["JwtSettings:Secret"];
        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException("JWT secret is not configured in appsettings.json under JwtSettings:Secret");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(secret);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userId.ToString())
        };

        if (!string.IsNullOrEmpty(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(CookieExpiryDays), // Original production value
            //Expires = DateTime.UtcNow.AddMinutes(CookieExpiryMinutes), // For testing
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
