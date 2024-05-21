using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using silicon_tokenProvider.Infrastructure.Models;
using silicon_tokenProvider.Infrastructure.Services;

namespace silicon_tokenProvider.Functions;

public class GenerateToken(ILogger<GenerateToken> logger, ITokenService refreshTokenService, ITokenGenerator tokenGenerator)
{
    private readonly ILogger<GenerateToken> _logger = logger;
    private readonly ITokenService _refreshTokenService = refreshTokenService;
    private readonly ITokenGenerator _tokenGenerator = tokenGenerator;

    [Function("GenerateToken")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "token/generate")] HttpRequest req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var tokenRequest = JsonConvert.DeserializeObject<TokenRequest>(body);

        if (tokenRequest == null || tokenRequest.UserId == null || tokenRequest.Email == null)
            return new BadRequestObjectResult(new { Error = "Please provide a valid user id and email" });
        
        try
        {
            RefreshTokenResult refreshTokenResult = null!;
            AccessTokenResult accessTokenResult = null!;

            using var ctsTimeOut = new CancellationTokenSource(TimeSpan.FromSeconds(120*1000));
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ctsTimeOut.Token, req.HttpContext.RequestAborted);

            req.HttpContext.Request.Cookies.TryGetValue("refreshToken", out var refreshToken);
            if (!string.IsNullOrEmpty(refreshToken))
                refreshTokenResult = await _refreshTokenService.GetRefreshTokenAsync(refreshToken, cts.Token);

            if (refreshTokenResult == null || refreshTokenResult.ExpiryDate < DateTime.Now.AddDays(1))
                refreshTokenResult = await _tokenGenerator.GenerateRefreshTokenAsync(tokenRequest.UserId, cts.Token);

            accessTokenResult = _tokenGenerator.GenerateAccessToken(tokenRequest, refreshTokenResult.Token);

            if(accessTokenResult != null && accessTokenResult.Token != null && refreshTokenResult.CookieOptions != null) //THIS IS WEIRD
                req.HttpContext.Response.Cookies.Append("refreshToken", refreshTokenResult.Token, refreshTokenResult.CookieOptions);

            if (accessTokenResult != null && accessTokenResult.Token != null && refreshTokenResult.Token != null)
                return new OkObjectResult(new { AccessToken = accessTokenResult.Token, RefreshToken = refreshTokenResult.Token});             
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Function(\"GenerateToken\")] :: {ex.Message}");
            return new ObjectResult(new { Error = $"Function GenerateToken failed :: {ex.Message}" }) { StatusCode = 500 };
        }

        return new ObjectResult(new { Error = "Function GenerateToken failed, no valid TokenResult" }) { StatusCode = 500 };
    }
}
