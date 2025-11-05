namespace Pinventory.Pins.Application.Commands;

public record AssignTags(Guid PinId, IReadOnlyCollection<string> Tags, string Reason, long ExpectedVersion);

public record UpdateVerificationStatus(Guid PinId, string ExistsStatus, DateTimeOffset CheckedAt, string Source, long ExpectedVersion);

public record StartTagging(string Scope, string ModelVersion);

public record ApplySuggestion(Guid JobId, Guid PinId, IReadOnlyCollection<string> Tags);

public record StartVerification(string Scope); // weekly or on-demand