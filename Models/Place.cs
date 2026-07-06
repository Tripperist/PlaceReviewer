namespace PlaceReviewer.Models;

public sealed record Place(
    string Name,
    string Category,
    string Location,
    string UserNotes);