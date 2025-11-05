using FluentResults;

using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Places;

public sealed class Pin(
    string ownerId,
    string name,
    GooglePlaceId googlePlaceId,
    Address address,
    Location location,
    DateTimeOffset addedAt,
    PinStatus status = PinStatus.Unknown,
    Guid? id = null) : AggregateRoot(id)
{
    private readonly HashSet<Tag> _tags = [];
    private Pin() : this(string.Empty, "no-name", GooglePlaceId.Unknown, Address.Unknown, Location.Default, DateTimeOffset.UtcNow) { }
    public string OwnerId { get; private set; } = ownerId;
    public string Name { get; private set; } = name;
    public GooglePlaceId PlaceId { get; private set; } = googlePlaceId;
    public Address Address { get; private set; } = address;
    public Location Location { get; private set; } = location;
    public PinStatus Status { get; private set; } = status;
    public DateTimeOffset StatusUpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset AddedAt { get; private set; } = addedAt;
    public IReadOnlyCollection<Tag> Tags => _tags;

    public Result<IEnumerable<Tag>> AssignTags(IEnumerable<string> tags, ITagVerifier tagVerifier)
    {
        var distinctTags = tags
            .Where(tag => tagVerifier.IsAllowed(OwnerId, tag))
            .ToList();

        _tags.Clear();
        List<Result<Tag>> results = [];
        foreach (var result in distinctTags.Select(Tag.Create))
        {
            results.Add(result);
            if (result.IsSuccess)
            {
                _tags.Add(result.Value);
            }
        }

        if (_tags.Any())
        {
            Raise(new Events.PinTagsAssigned(Id, _tags.Select(t => t.Value)));
        }

        return results.Any(r => !r.IsSuccess) ? Result.Merge(results.ToArray()) : Result.Ok();
    }

    public Result<Success> Close(bool isTemporary = false)
    {
        return Status switch
        {
            PinStatus.Open or PinStatus.TemporaryClosed => DoClose(),
            PinStatus.Closed => Result.Ok(),
            _ => Result.Fail(Errors.Pin.PinCannotBeClosed(Status))
        };

        Result<Success> DoClose()
        {
            var status = isTemporary ? PinStatus.TemporaryClosed : PinStatus.Closed;
            if (Status != status)
            {
                var previousStatus = Status;
                Status = status;
                StatusUpdatedAt = DateTimeOffset.UtcNow;
                Raise(new Events.PinClosed(Id, Status, previousStatus));
            }

            return Result.Ok();
        }
    }

    public Result<Success> Open()
    {
        return Status switch
        {
            PinStatus.Unknown or PinStatus.TemporaryClosed => DoOpen(),
            PinStatus.Open => Result.Ok(),
            _ => Result.Fail(Errors.Pin.PinCannotBeOpened(Status))
        };

        Result<Success> DoOpen()
        {
            var previousStatus = Status;
            Status = PinStatus.Open;
            StatusUpdatedAt = DateTimeOffset.UtcNow;
            Raise(new Events.PinOpened(Id, Status, previousStatus));
            return Result.Ok();
        }
    }

    public void Rename(string name)
    {
        if (!string.IsNullOrWhiteSpace(name) && Name != name)
        {
            Name = name;
            StatusUpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}