using System.Text;
using System.Text.Json;

using Google;

namespace Pinventory.Pins.Import.Worker.DataPortability;

public static class GoogleApiExceptionExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public static GoogleErrorResponse? ToGoogleErrorResponse(this GoogleApiException exception)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(exception.Error.ErrorResponseContent));
        return JsonSerializer.Deserialize<GoogleErrorResponse>(stream, JsonOptions);
    }

    public static string ExtractArchiveJobId(this GoogleErrorResponse errorResponse)
    {
        return errorResponse.Error.Details.FirstOrDefault()?.Metadata.JobId ?? string.Empty;
    }

    public record GoogleErrorResponse(GoogleError Error);

    public record GoogleError(string Status, ErrorDetails[] Details);

    public record ErrorDetails(Metadata Metadata);

    public record Metadata(string JobId, string AccessType, string TimestampAfter24Hrs);
}