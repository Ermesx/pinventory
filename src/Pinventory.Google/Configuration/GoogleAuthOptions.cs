using System.ComponentModel.DataAnnotations;

namespace Pinventory.Google.Configuration;

public class GoogleAuthOptions
{
    [Required]
    public required string ClientId { get; set; }

    [Required]
    public required string ClientSecret { get; set; }
}