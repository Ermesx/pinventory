namespace Pinventory.Pins.Application.Commands;

public record ImportOrUpdatePin(
    string GooglePlaceId,
    string Name,
    string? Address,
    double Lat,
    double Lng,
    DateTimeOffset? StarredAt,
    string IdempotencyKey);

public record AssignTags(Guid PinId, IReadOnlyCollection<string> Tags, string Reason, long ExpectedVersion);

public record UpdateVerificationStatus(Guid PinId, string ExistsStatus, DateTimeOffset CheckedAt, string Source, long ExpectedVersion);

public record StartImport(Guid UserId, string Source, string? Cursor, string IdempotencyKey);
public record AppendBatchStats(Guid ImportJobId, int Processed, int Created, int Updated, int Failed, int Conflicts);
public record CompleteImport(Guid ImportJobId);
public record FailImport(Guid ImportJobId, string Error);

public record StartTagging(string Scope, string ModelVersion);
public record ApplySuggestion(Guid JobId, Guid PinId, IReadOnlyCollection<string> Tags);
public record StartVerification(string Scope); // weekly or on-demand