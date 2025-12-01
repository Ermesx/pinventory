namespace Pinventory.Pins.Domain.Importing.Events;

public static class EnumerableEventsExtensions
{
    public static (Guid importId, string userId, string archiveJobId) GetIdentifiers(this IEnumerable<ImportEvent> events)
    {
        var first = events.First();
        return (first.Id, first.UserId, first.ArchiveJobId);
    }
}