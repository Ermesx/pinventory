namespace Pinventory.Pins.Application.Importing.Messages;

public interface ICorrelatedMessage
{
    string UserId { get; }
    string ArchiveJobId { get; }
}