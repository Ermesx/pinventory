using System.ComponentModel.DataAnnotations;

namespace Pinventory.ServiceDefaults;

public record PinventoryOptions
{
    public const string Section = "Pinventory";

    [Required]
    public required string AdminId { get; init; }
}