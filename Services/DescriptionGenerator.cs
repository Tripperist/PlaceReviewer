using OpenAI.Chat;
using PlaceReviewer.Models;
using PlaceReviewer.Prompts;

namespace PlaceReviewer.Services;

public sealed class DescriptionGenerator(ChatClient chatClient) : IDescriptionGenerator
{
    private static readonly ChatCompletionOptions GenerationOptions = new()
    {
        Temperature = 0.2f
    };

    public async Task<string> GenerateAsync(
        Place place,
        CancellationToken cancellationToken = default)
    {
        ChatCompletion completion = await chatClient.CompleteChatAsync(
            messages:
            [
                new SystemChatMessage(DescriptionPrompt.System),
                new UserChatMessage(DescriptionPrompt.User(place))
            ],
            options: GenerationOptions,
            cancellationToken: cancellationToken);
            
        return completion.Content[0].Text;
    }
}
