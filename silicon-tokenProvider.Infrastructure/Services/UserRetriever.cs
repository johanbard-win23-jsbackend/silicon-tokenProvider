using Microsoft.EntityFrameworkCore;
using silicon_tokenProvider.Infrastructure.Data.Contexts;
using silicon_tokenProvider.Infrastructure.Models;
using System.Threading;

namespace silicon_tokenProvider.Infrastructure.Services;

public interface IUserRetriever
{
    Task<GetUserResult> GetUserAsync(string refreshToken, CancellationToken cts);
}

public class UserRetriever(IDbContextFactory<DataContext> dbContextFactory) : IUserRetriever
{
    private readonly IDbContextFactory<DataContext> _dbContextFactory = dbContextFactory;

    public async Task<GetUserResult> GetUserAsync(string authToken, CancellationToken cts)
    {
        try
        {
            await using var context = _dbContextFactory.CreateDbContext();

            var results = await context.RefreshTokens.Where(x => x.RefreshToken == authToken).ToListAsync(cancellationToken);

            foreach (var res in results)
            {
                if (res != null && res.RefreshToken != null && res.ExpiryDate > DateTime.Now)
                    return new GetUserResult
                    {
                        StatusCode = 200,
                        UserId = res.UserId
                    };
            }

        }
        catch (Exception ex)
        {
            return new GetUserResult
            {
                StatusCode = 500,
                Error = $"GetUserResult :: {ex.Message}"
            };
        }

        return new GetUserResult
        {
            StatusCode = 401,
            Error = "(res != null && res.RefreshToken != null && res.ExpiryDate > DateTime.Now) failed"
        };
    }
}
