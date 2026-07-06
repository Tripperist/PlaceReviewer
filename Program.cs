using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using OpenAI;
using OpenAI.Chat;

using PlaceReviewer.Configuration;
using PlaceReviewer.Models;
using PlaceReviewer.Services;

using System.ClientModel;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

builder.Services
    .AddOptions<OpenRouterOptions>()
    .BindConfiguration(OpenRouterOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(sp =>
{
    OpenRouterOptions options = sp
        .GetRequiredService<IOptions<OpenRouterOptions>>()
        .Value;

    return new ChatClient(
        options.Model,
        new ApiKeyCredential(options.ApiKey),
        new OpenAIClientOptions
        {
            Endpoint = new Uri(options.Endpoint)
        });
});

builder.Services.AddSingleton<IDescriptionGenerator, DescriptionGenerator>();

try
{
    using IHost host = builder.Build();

    var generator = host.Services.GetRequiredService<IDescriptionGenerator>();

    Place[] places = args.Contains("--eval", StringComparer.OrdinalIgnoreCase)
        ? GetPromptEvaluationPlaces()
        :
        [
            new(
                Name: "Cape Disappointment State Park",
                Category: "Nature",
                Location: "Ilwaco, Washington",
                UserNotes: """
                    Beautiful coastal park with dramatic cliffs.
                    Historic lighthouse.
                    Great place to watch storms and sunsets.
                    """)
        ];

    bool hasGenerationFailures = false;

    foreach (Place place in places)
    {
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"{place.Name} ({place.Category})");
        Console.WriteLine(place.Location);
        Console.WriteLine();

        try
        {
            string markdown = await generator.GenerateAsync(place);

            Console.WriteLine(markdown);
            Console.WriteLine();
        }
        catch (ClientResultException ex)
        {
            hasGenerationFailures = true;

            Console.Error.WriteLine($"Generation failed: HTTP {ex.Status}");
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine();
        }
    }

    if (hasGenerationFailures)
    {
        Environment.ExitCode = 1;
    }
}
catch (OptionsValidationException ex)
{
    Console.Error.WriteLine("OpenRouter configuration is incomplete.");

    foreach (string failure in ex.Failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    Console.Error.WriteLine();
    Console.Error.WriteLine("Set your API key with:");
    Console.Error.WriteLine("dotnet user-secrets set \"OpenRouter:ApiKey\" \"YOUR_OPENROUTER_API_KEY\"");

    Environment.ExitCode = 1;
}

static Place[] GetPromptEvaluationPlaces() =>
[
    new(
        Name: "Cape Disappointment State Park",
        Category: "Nature",
        Location: "Ilwaco, Washington",
        UserNotes: """
            Beautiful coastal park with dramatic cliffs.
            Historic lighthouse.
            Great place to watch storms and sunsets.
            """),

    new(
        Name: "Pike Place Chowder",
        Category: "Food & Drink",
        Location: "Seattle, Washington",
        UserNotes: """
            Small counter-service stop near Pike Place Market.
            The line moved faster than expected.
            Clam chowder was rich and comforting on a rainy afternoon.
            Good quick lunch while walking around downtown.
            """),

    new(
        Name: "The Bradbury Building",
        Category: "Architecture",
        Location: "Los Angeles, California",
        UserNotes: """
            The lobby has ornate ironwork, open elevators, and a dramatic skylit atrium.
            Worth a short stop if you like old buildings and interior details.
            Photos came out well from the ground floor.
            """),

    new(
        Name: "Vietnam Veterans Memorial",
        Category: "Memorial",
        Location: "Washington, D.C.",
        UserNotes: """
            Quiet, reflective place.
            The dark wall and engraved names made the visit feel personal.
            Better suited for a slow stop than a quick photo.
            """),

    new(
        Name: "Pullout Overlook",
        Category: "Scenic View",
        Location: "Highway 101",
        UserNotes: """
            Nice view.
            """)
];
