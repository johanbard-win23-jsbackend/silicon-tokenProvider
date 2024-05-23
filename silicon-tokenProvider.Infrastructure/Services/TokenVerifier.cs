using Microsoft.EntityFrameworkCore;
using silicon_tokenProvider.Infrastructure.Data.Contexts;
using silicon_tokenProvider.Infrastructure.Models;

namespace silicon_tokenProvider.Infrastructure.Services;

public interface ITokenVerifier
{
    Task<VerifyTokenResult> VerifyTokenAsync(string authToken, CancellationToken cancellationToken);
}

public class TokenVerifier(IDbContextFactory<DataContext> dbContextFactory) : ITokenVerifier
{
    private readonly IDbContextFactory<DataContext> _dbContextFactory = dbContextFactory;

    public async Task<VerifyTokenResult> VerifyTokenAsync(string authToken, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = _dbContextFactory.CreateDbContext();

            var results = await context.RefreshTokens.Where(x => x.RefreshToken == authToken).ToListAsync(cancellationToken);

            foreach (var res in results)
            {
                if (res != null && res.RefreshToken != null && res.ExpiryDate > DateTime.Now)
                    return new VerifyTokenResult
                    {
                        StatusCode = 200,
                    };
            }
            
        }
        catch(Exception ex)
        {
            return new VerifyTokenResult
            {
                StatusCode = 500,
                Error = $"VerifyTokenResult :: {ex.Message}"
            };
        }

        return new VerifyTokenResult
        {
            StatusCode = 401,
            Error = "(res != null && res.RefreshToken != null && res.ExpiryDate > DateTime.Now) failed"
        };
    }
}
