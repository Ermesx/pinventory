using Pinventory.Pins.Infrastructure.Messages;

namespace Pinventory.Pins.Application.Importing.Messages;

public record DownloadArchiveMessage(Guid ImportId, string UserId, string ArchiveJobId, IList<string> Urls) : IUserMessage
{
    public static DownloadArchiveMessage Create(CheckJobMessage message, IList<string> urls) =>
        new(message.ImportId, message.UserId, message.ArchiveJobId, urls);
}