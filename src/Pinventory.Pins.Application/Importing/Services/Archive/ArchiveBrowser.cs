namespace Pinventory.Pins.Application.Importing.Services.Archive;

public record ArchiveBrowser(string CreationTime, string TotalSize, ServiceStatus[] ServiceStatus);

public record ServiceStatus(
    Service Service,
    string LocalizedServiceName,
    bool IsIncomplete,
    ExtractedFile[] ExtractedFile,
    string FolderName,
    ServiceInformation ServiceInformation,
    string DeletedFileCount,
    string ExtractedFileTotal,
    string ExtractedFileSize);

public record Service(string Name);

public record ExtractedFile(string Name, string Extension, string State, string UrlSafeName);

public record ServiceInformation(
    string Name,
    BriefDescription BriefDescription,
    PromoText PromoText,
    FolderStructure FolderStructure,
    ReviewUrls[] ReviewUrls,
    ObjectsExported[] ObjectsExported);

public record BriefDescription(string PrivateDoNotAccessOrElseSafeHtmlWrappedValue);

public record PromoText(string PrivateDoNotAccessOrElseSafeHtmlWrappedValue);

public record FolderStructure(string PrivateDoNotAccessOrElseSafeHtmlWrappedValue);

public record ReviewUrls(Description Description, string Url);

public record Description(string PrivateDoNotAccessOrElseSafeHtmlWrappedValue);

public record ObjectsExported(string Name, ShortExplanation ShortExplanation, OutputFormats[] OutputFormats);

public record ShortExplanation(string PrivateDoNotAccessOrElseSafeHtmlWrappedValue);

public record OutputFormats(string Name, ShortExplanation1 ShortExplanation, LongExplanation LongExplanation);

public record ShortExplanation1(string PrivateDoNotAccessOrElseSafeHtmlWrappedValue);

public record LongExplanation(string PrivateDoNotAccessOrElseSafeHtmlWrappedValue);