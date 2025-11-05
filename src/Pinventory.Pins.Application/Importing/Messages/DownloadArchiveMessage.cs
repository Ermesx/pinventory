namespace Pinventory.Pins.Application.Importing.Messages;

public record DownloadArchiveMessage(string UserId, string ArchiveJobId, IList<string> Urls) : ICorrelatedMessage
{
    public static DownloadArchiveMessage Create(ICorrelatedMessage message, IList<string> urls) =>
        new(message.UserId, message.ArchiveJobId, urls);
}