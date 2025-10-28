using System.ComponentModel.DataAnnotations;

namespace Pinventory.Google.Configuration;

public record GooglePlatformOptions
{
    public const string Section = "Google:Platform";

    [Required]
    public required string ProjectId { get; init; }
}