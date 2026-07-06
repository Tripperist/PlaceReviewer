using System.ComponentModel.DataAnnotations;

namespace PlaceReviewer.Configuration;

public sealed class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    [Required]
    public required string Endpoint { get; init; }

    [Required]
    public required string Model { get; init; }

    [Required]
    public required string ApiKey { get; init; }
}