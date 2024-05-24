using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using silicon_tokenProvider.Infrastructure.Services;

namespace silicon_tokenProvider.Functions;

public class GetUserFromToken
{
    private readonly ILogger<GetUserFromToken> _logger;
    private readonly IUserRetriever _userRetriever;

    public GetUserFromToken(ILogger<GetUserFromToken> logger, IUserRetriever userRetriever)
    {
        _logger = logger;
        _userRetriever = userRetriever;
    }

    [Function("GetUserFromToken")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        var authToken = req.Headers.Authorization.ToString();

        try
        {
            if (authToken != null)
            {
                int i = authToken.IndexOf(" ") + 1;
                authToken = authToken.Substring(i);

                using var ctsTimeOut = new CancellationTokenSource(TimeSpan.FromSeconds(120 * 1000));
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ctsTimeOut.Token, req.HttpContext.RequestAborted);

                var res = await _userRetriever.GetUserAsync(authToken, cts.Token);

                if (res.StatusCode == 200)
                {
                    return new OkObjectResult(new { StatusCode = 200, UserId = res.UserId });
                }

                else if (res.StatusCode == 401)
                {
                    _logger.LogWarning(res.Error);
                    return new UnauthorizedResult();
                }
                else if (res.StatusCode == 500)
                {
                    _logger.LogWarning(res.Error);
                    return new ObjectResult(new { Error = res.Error! }) { StatusCode = 500 };
                }
                else
                {
                    _logger.LogWarning("GetUserFromToken :: Unknown error");
                    return new ObjectResult(new { Error = res.Error! }) { StatusCode = 500 };
                }

            }
        }
        catch (Exception ex)
        {
            return new ObjectResult(new { Error = $"Function GetUserFromToken failed :: {ex.Message}" }) { StatusCode = 500 };
        }

        return new ObjectResult(new { Error = $"Function GetUserFromToken failed" }) { StatusCode = 500 };
    }
}
