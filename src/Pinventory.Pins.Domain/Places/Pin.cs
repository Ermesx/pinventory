using System.Collections.Immutable;

using FluentResults;

using Pinventory.Pins.Domain.Abstractions;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Places.Events;

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
    public IReadOnlyCollection<Tag> Tags => _tags.ToImmutableHashSet();

    public static Pin Create(string ownerId, GooglePlaceId placeId, StarredPlace place)
    {
        return new Pin(ownerId,
            place.Name!,
            placeId,
            Address.From(place.Address!, place.CountryCode!.Value),
            Location.From(place.Latitude!.Value, place.Longitude!.Value),
            place.AddedDate);
    }

    public async Task<Result<IEnumerable<Tag>>> AssignTagsAsync(IEnumerable<string> tags, ITagVerifier tagVerifier,
        CancellationToken cancellationToken = default)
    {
        var verificationTasks = tags.Select(async tag => new
        {
            Tag = tag, IsAllowed = await tagVerifier.IsAllowedAsync(OwnerId, tag, cancellationToken)
        });

        var verifiedTags = await Task.WhenAll(verificationTasks);
        var distinctTags = verifiedTags
            .Where(x => x.IsAllowed)
            .Select(x => x.Tag)
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
            Raise(new PinTagsAssigned(Id, _tags.Select(t => t.Value)));
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
                Raise(new PinClosed(Id, Status, previousStatus));
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
            Raise(new PinOpened(Id, Status, previousStatus));
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