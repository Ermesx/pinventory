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
        // Clear the change tracker first to detach all entities
        DbContext.ChangeTracker.Clear();

        // Get all table names from the model (including owned entities)
        var tableNames = DbContext.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .Where(name => name != null)
            .Distinct()
            .ToList();

        if (tableNames.Any())
        {
            try
            {
                var schema = DbContext.Model.GetDefaultSchema() ?? "public";

                foreach (var tableName in tableNames)
                {
                    // Table names come from EF Core model metadata (not user input), so this is safe
                    await DbContext.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{schema}\".\"{tableName}\" CASCADE");
                }
            }
            catch
            {
                // Tables don't exist yet or other error, ignore
            }
        }
    }
}