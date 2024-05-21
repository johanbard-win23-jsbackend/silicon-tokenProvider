using Microsoft.IdentityModel.Tokens;
using silicon_tokenProvider.Infrastructure.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace silicon_tokenProvider.Infrastructure.Services;

public interface ITokenGenerator
{
    Task<RefreshTokenResult> GenerateRefreshTokenAsync(string userId, CancellationToken cancellationToken);
    AccessTokenResult GenerateAccessToken(TokenRequest tokenRequest, string? refreshToken);
}

public class TokenGenerator(ITokenService refreshTokenService) : ITokenGenerator
{
    private readonly ITokenService _refreshTokenService = refreshTokenService;

    #region GenerateRefreshTokenAsync
    public async Task<RefreshTokenResult> GenerateRefreshTokenAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
                return new RefreshTokenResult { StatusCode = (int)HttpStatusCode.BadRequest, Error = "No UserId" };

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };

            var token = GenerateJwtToken(new ClaimsIdentity(claims), 5);
            if (token == null)
                return new RefreshTokenResult { StatusCode = (int)HttpStatusCode.InternalServerError, Error = "No token generated" };

            var cookieOptions = CookieGenerator.GenerateCookie(DateTimeOffset.Now.AddDays(7));
            if (cookieOptions == null)
                return new RefreshTokenResult { StatusCode = (int)HttpStatusCode.InternalServerError, Error = "No cookie generated" };

            try
            {
                var result = await _refreshTokenService.SaveRefreshTokenAsync(token, userId, cancellationToken);
                if (!result)
                    return new RefreshTokenResult { StatusCode = (int)HttpStatusCode.InternalServerError, Error = "Token not saved to database" };
            }
            catch (Exception ex)
            {
                return new RefreshTokenResult { StatusCode = (int)HttpStatusCode.InternalServerError, Error = ex.Message };
            }

            return new RefreshTokenResult
            {
                StatusCode = (int)HttpStatusCode.OK,
                Token = token,
                CookieOptions = cookieOptions
            };
        }
        catch (Exception ex)
        {
            return new RefreshTokenResult { StatusCode = (int)HttpStatusCode.InternalServerError, Error = ex.Message };
        }
    }
    #endregion

    #region GenerateAccessToken
    public AccessTokenResult GenerateAccessToken(TokenRequest tokenRequest, string? refreshToken)
    {
        try
        {
            if (string.IsNullOrEmpty(tokenRequest.UserId) || string.IsNullOrEmpty(tokenRequest.Email))
                return new AccessTokenResult { StatusCode = (int)HttpStatusCode.BadRequest, Error = "No UserId or Email" };

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, tokenRequest.UserId),
                new Claim(ClaimTypes.Name, tokenRequest.Email),
                new Claim(ClaimTypes.Email, tokenRequest.Email)
            };

            if (!string.IsNullOrEmpty(refreshToken))
                claims = [.. claims, new Claim("refreshToken", refreshToken)];

            var token = GenerateJwtToken(new ClaimsIdentity(claims), 5);

            if (token == null)
                return new AccessTokenResult { StatusCode = (int)HttpStatusCode.InternalServerError, Error = "No token generated" };

            return new AccessTokenResult
            {
                StatusCode = (int)HttpStatusCode.OK,
                Token = token
            };
        }
        catch (Exception ex)
        {
            return new AccessTokenResult { StatusCode = (int)HttpStatusCode.InternalServerError, Error = ex.Message };
        }
    }

    #endregion

    #region GenerateJwtToken
    public static string GenerateJwtToken(ClaimsIdentity claims, double expireMin = 5)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claims,
            Expires = DateTime.Now.AddMinutes(expireMin),
            Issuer = Environment.GetEnvironmentVariable("TokenIssuer"),
            Audience = Environment.GetEnvironmentVariable("TokenAudience"),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("TokenSecret")!)), SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    #endregion
}
