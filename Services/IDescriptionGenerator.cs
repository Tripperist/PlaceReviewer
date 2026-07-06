using PlaceReviewer.Models;

namespace PlaceReviewer.Services;

public interface IDescriptionGenerator
{
    Task<string> GenerateAsync(
        Place place,
        CancellationToken cancellationToken = default);
}