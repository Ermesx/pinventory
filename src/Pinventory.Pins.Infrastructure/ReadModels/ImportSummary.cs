using Pinventory.Pins.Domain.Importing;

namespace Pinventory.Pins.Infrastructure.ReadModels;

public sealed class ImportSummary
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string? ArchiveJobId { get; init; }
    public ImportState State { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset PeriodStart { get; init; }
    public DateTimeOffset PeriodEnd { get; init; }
    public int Total { get; init; }
    public int Processed { get; init; }
    public int Created { get; init; }
    public int Updated { get; init; }
    public int Failed { get; init; }
    public int Conflicts { get; init; }
}