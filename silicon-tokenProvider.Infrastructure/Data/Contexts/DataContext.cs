using Microsoft.EntityFrameworkCore;
using silicon_tokenProvider.Infrastructure.Data.Entities;

namespace silicon_tokenProvider.Infrastructure.Data.Contexts;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }
}
