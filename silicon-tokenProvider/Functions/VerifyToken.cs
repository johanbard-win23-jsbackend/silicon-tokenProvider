using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using silicon_tokenProvider.Infrastructure.Models;
using silicon_tokenProvider.Infrastructure.Services;

namespace silicon_tokenProvider.Functions
{
    public class VerifyToken
    {
        private readonly ILogger<VerifyToken> _logger;
        private readonly ITokenVerifier _tokenVerifier;

        public VerifyToken(ILogger<VerifyToken> logger, ITokenVerifier tokenVerifier)
        {
            _logger = logger;
            _tokenVerifier = tokenVerifier;
        }

        [Function("VerifyToken")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "token/verify")] HttpRequest req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var verifyTokenRequest = JsonConvert.DeserializeObject<VerifyTokenRequest>(body);
            
            try
            {
                if (verifyTokenRequest != null && verifyTokenRequest.AccessToken != null && verifyTokenRequest.RefreshToken != null)
                {
                    _logger.LogWarning(verifyTokenRequest.AccessToken);
                    _logger.LogWarning(verifyTokenRequest.RefreshToken);

                    using var ctsTimeOut = new CancellationTokenSource(TimeSpan.FromSeconds(120 * 1000));
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ctsTimeOut.Token, req.HttpContext.RequestAborted);

                    var res = await _tokenVerifier.VerifyTokenAsync(verifyTokenRequest.RefreshToken, cts.Token);

                    if (res.StatusCode == 200)
                        return new OkResult();
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
                        _logger.LogWarning("VerifyToken :: Unknown error");
                        return new ObjectResult(new { Error = res.Error! }) { StatusCode = 500 };
                    }

                }
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { Error = $"Function GenerateToken failed :: {ex.Message}" }) { StatusCode = 500 };
            }

            return new ObjectResult(new { Error = $"Function VerifyToken failed" }) { StatusCode = 500 };
        }
    }
}
