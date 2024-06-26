﻿using Microsoft.EntityFrameworkCore;
using silicon_tokenProvider.Infrastructure.Data.Contexts;
using silicon_tokenProvider.Infrastructure.Data.Entities;
using silicon_tokenProvider.Infrastructure.Models;
using System.Net;

namespace silicon_tokenProvider.Infrastructure.Services;

public interface ITokenService
{
    Task<RefreshTokenResult> GetRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task<RefreshTokenResult> GetRefreshTokenIdAsync(string userId, CancellationToken cancellationToken);
    Task<bool> SaveRefreshTokenAsync(string refreshToken, string UserId, CancellationToken cancellationToken);
}

public class TokenService(IDbContextFactory<DataContext> dbContextFactory) : ITokenService
{
    private readonly IDbContextFactory<DataContext> _dbContextFactory = dbContextFactory;

    #region GetRefreshTokenAsync

    public async Task<RefreshTokenResult> GetRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        RefreshTokenResult refreshTokenResult = null!;

        try
        {
            var refreshTokenEntity = await context.RefreshTokens.FirstOrDefaultAsync(x => x.RefreshToken == refreshToken && x.ExpiryDate > DateTime.Now, cancellationToken);
            if (refreshTokenEntity != null)
            {
                refreshTokenResult = new RefreshTokenResult
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Token = refreshTokenEntity.RefreshToken,
                    ExpiryDate = refreshTokenEntity.ExpiryDate
                };
            }
            else
            {
                refreshTokenResult = new RefreshTokenResult
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Error = "Refresh token not found or expired"
                };
            }            
        }
        catch (Exception ex)
        {
            refreshTokenResult = new RefreshTokenResult
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Error = $"GetRefreshTokenAsync failed {ex.Message}"
            };
        }
        return refreshTokenResult;

    }

    #endregion

    #region GetRefreshTokenIdAsync
    public async Task<RefreshTokenResult> GetRefreshTokenIdAsync(string userId, CancellationToken cancellationToken)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        RefreshTokenResult refreshTokenResult = null!;

        try
        {
            var refreshTokenEntity = await context.RefreshTokens.FirstOrDefaultAsync(x => x.UserId == userId && x.ExpiryDate > DateTime.Now, cancellationToken);
            if (refreshTokenEntity != null)
            {
                refreshTokenResult = new RefreshTokenResult
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Token = refreshTokenEntity.RefreshToken,
                    ExpiryDate = refreshTokenEntity.ExpiryDate
                };
            }
            else
            {
                refreshTokenResult = new RefreshTokenResult
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Error = "Refresh token not found or expired"
                };
            }
        }
        catch (Exception ex)
        {
            refreshTokenResult = new RefreshTokenResult
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Error = $"GetRefreshTokenAsync failed :: {ex.Message}"
            };
        }
        return refreshTokenResult;
    }
    #endregion

    #region SaveRefreshTokenAsync

    public async Task<bool> SaveRefreshTokenAsync(string refreshToken, string userId, CancellationToken cancellationToken)
    {
        try
        {
            //var tokenLifetime = double.TryParse(Environment.GetEnvironmentVariable("RefreshTokenLifetime"), out double refreshTokenLifeTime) ? refreshTokenLifetime : 7;
            double refreshTokenLifeTime = 7;

            await using var context = _dbContextFactory.CreateDbContext();
            var refreshTokenEntity = new RefreshTokenEntity()
            {
                RefreshToken = refreshToken,
                UserId = userId,
                ExpiryDate = DateTime.Now.AddDays(refreshTokenLifeTime)
            };

            context.RefreshTokens.Add(refreshTokenEntity);
            await context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
        
    }

    #endregion
}
