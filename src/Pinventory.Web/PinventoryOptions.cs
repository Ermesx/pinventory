using System.ComponentModel.DataAnnotations;

namespace Pinventory.Web;

public class PinventoryOptions
{
    public const string Section = "Pinventory";

    [Required]
    public required string ProjectId { get; init; }
}