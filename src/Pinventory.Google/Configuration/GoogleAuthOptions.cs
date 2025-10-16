using System.ComponentModel.DataAnnotations;

namespace Pinventory.Google.Configuration;

public record GoogleAuthOptions
{
    public const string Section = "Authentication:Google";

    [Required]
    public required string ClientId { get; init; }

    [Required]
    public required string ClientSecret { get; init; }
}