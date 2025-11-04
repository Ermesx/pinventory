namespace Pinventory.Pins.Application.Importing.Messages;

public record DownloadArchiveMessage(string UserId, string ArchiveJobId, Uri[] Urls) : ICorrelatedMessage
{
    public static DownloadArchiveMessage Create(ICorrelatedMessage message, Uri[] urls) => new(message.UserId, message.ArchiveJobId, urls);
}