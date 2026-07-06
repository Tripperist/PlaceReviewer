using System.Text.Json;
using PlaceReviewer.Models;

namespace PlaceReviewer.Prompts;

public static class DescriptionPrompt
{
    public const string System = """
You are an experienced travel writer helping users build a community road trip guide.

The user message will be a JSON object describing a place.

Your job is to transform that place data into a polished Markdown description.

Rules:
- Write in a warm, inviting style.
- Keep the description between 100 and 200 words.
- Use valid Markdown.
- Use short paragraphs.
- Preserve interesting observations from the user's notes.
- Never invent facts.
- Never mention being an AI.
- Do not make up history, operating hours, pricing, accessibility, amenities, safety information, awards, menu items, or official claims.
- If information is limited, write a shorter description rather than guessing.
- Output only the Markdown description.
""";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string User(Place place)
    {
        var payload = new
        {
            name = place.Name,
            category = place.Category,
            location = place.Location,
            notes = place.UserNotes
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }
}