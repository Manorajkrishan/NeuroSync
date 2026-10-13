using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NeuroSync.Api.Data;

/// <summary>Used by <c>dotnet ef</c>. Runtime selection is in <c>LiveBetaComposition</c>.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NeuroSyncDbContext>
{
    public NeuroSyncDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("NEUROSYNC_DESIGN_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set NEUROSYNC_DESIGN_CONNECTION to a Postgres connection string before running dotnet ef. Keep that value out of the repo.");
        }

        var options = new DbContextOptionsBuilder<NeuroSyncDbContext>();
        options.UseNpgsql(connectionString);
        return new NeuroSyncDbContext(options.Options);
    }
}
