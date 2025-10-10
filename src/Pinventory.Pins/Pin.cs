using System.ComponentModel.DataAnnotations;

using Pinventory.Pins.Import;

namespace Pinventory.Pins;

public sealed class Pin
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public GooglePlaceId GooglePlaceId { get; private set; }
    public string Name { get; private set; } = default!;

    public Address? Address { get; private set; }
    public Location Location { get; private set; }
    public ExistsStatus ExistsStatus { get; private set; } = ExistsStatus.Unknown;
    public DateTimeOffset? ExistsCheckedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    // Optimistic concurrency token: managed by EF Core; do not modify in domain logic
    public long Version { get; private set; }

    private readonly List<PinTag> _tags = new();
    public IReadOnlyCollection<PinTag> Tags => _tags;

    private Pin() { }

    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents;
    private void Raise(object @event) => _domainEvents.Add(@event);

    public static Pin ImportOrUpdate(GooglePlaceId googlePlaceId, string name, Address? address, Location location,
        DateTimeOffset? starredAtUtc = null)
    {
        if (googlePlaceId.IsEmpty) throw new ArgumentException("GooglePlaceId is required", nameof(googlePlaceId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required", nameof(name));

        var now = DateTimeOffset.UtcNow;
        var pin = new Pin
        {
            GooglePlaceId = googlePlaceId,
            Name = name.Trim(),
            Address = address,
            Location = location,
            UpdatedAt = starredAtUtc ?? now
        };
        pin.Raise(new Events.PinImported(pin.Id, pin.GooglePlaceId));
        return pin;
    }

    public void UpdateCore(string name, Address? address, Location location, DateTimeOffset updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required", nameof(name));
        Name = name.Trim();
        Address = address;
        Location = location;
        UpdatedAt = updatedAtUtc;
        // Version increments are managed by EF Core's concurrency token mechanism
        Raise(new Events.PinUpdated(Id));
    }

    public void AssignTags(IEnumerable<string> tags, string reason)
    {
        if (tags is null) throw new ArgumentNullException(nameof(tags));
        var distinct = tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct().ToArray();
        _tags.Clear();
        foreach (var t in distinct)
            _tags.Add(new PinTag(Id, t));
        UpdatedAt = DateTimeOffset.UtcNow;
        // Version increments are managed by EF Core's concurrency token mechanism
        Raise(new Events.PinTagsAssigned(Id, distinct, reason));
    }

    public void UpdateVerification(ExistsStatus status, DateTimeOffset checkedAt, string source)
    {
        ExistsStatus = status;
        ExistsCheckedAt = checkedAt;
        UpdatedAt = DateTimeOffset.UtcNow;
        // Version increments are managed by EF Core's concurrency token mechanism
        Raise(new Events.PinVerificationUpdated(Id, status.ToString(), checkedAt, source));
    }

    public void MarkDuplicateCandidate(Guid otherPinId, string evidence)
    {
        /* event only, no state */
    }

    public static Pin Merge(Pin keep, Pin remove, string mergeStrategy)
    {
        /* domain rules */
        return keep;
    }
}