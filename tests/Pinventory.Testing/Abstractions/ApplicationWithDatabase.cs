using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Pinventory.Testing.Abstractions;

public abstract class ApplicationWithDatabase<TDbContext> where TDbContext : DbContext
{
    public TDbContext DbContext { get; protected set; } = null!;
    
    protected async Task InitializeDatabaseAsync(TDbContext dbContext)
    {
        DbContext = dbContext;
        await DbContext.Database.EnsureCreatedAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        var entityTypes = DbContext.Model.GetEntityTypes();
        // Clear all data from tables (if they exist)
        try
        {
            foreach (IEntityType entityType in entityTypes)
            {
                var tableName = entityType.GetTableName();
                await DbContext.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE pins.{tableName} CASCADE");
            }
        }
        catch
        {
            // Tables don't exist yet, ignore
        }
    }
}