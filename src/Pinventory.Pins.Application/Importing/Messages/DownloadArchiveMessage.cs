namespace Pinventory.Pins.Application.Importing.Messages;

public record DownloadArchiveMessage(Guid ImportId, string UserId, string ArchiveJobId, IList<string> Urls)
{
    public string UserId { get; set; } = UserId;

    public static DownloadArchiveMessage Create(CheckJobMessage message, IList<string> urls) =>
        new(message.ImportId, message.UserId, message.ArchiveJobId, urls);
}