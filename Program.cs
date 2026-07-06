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
using System.Text.Json;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

const string DefaultEvaluationFilePath = "places.json";

JsonSerializerOptions placeJsonOptions = new()
{
    AllowTrailingCommas = true,
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip
};

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
    Place[] places = GetPlaces(args, placeJsonOptions);

    using IHost host = builder.Build();

    var generator = host.Services.GetRequiredService<IDescriptionGenerator>();

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
        catch (InvalidOperationException ex)
        {
            hasGenerationFailures = true;

            Console.Error.WriteLine($"Generation failed: {ex.Message}");
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
catch (ArgumentException ex) when (ex.ParamName is null)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  dotnet run");
    Console.Error.WriteLine("  dotnet run -- --eval");
    Console.Error.WriteLine("  dotnet run -- --eval places.json");
    Console.Error.WriteLine("  dotnet run -- --places places.json");

    Environment.ExitCode = 1;
}
catch (IOException ex)
{
    Console.Error.WriteLine($"Could not read places file: {ex.Message}");

    Environment.ExitCode = 1;
}
catch (JsonException ex)
{
    Console.Error.WriteLine($"Could not parse places file: {ex.Message}");

    Environment.ExitCode = 1;
}

static Place[] GetPlaces(string[] args, JsonSerializerOptions jsonOptions)
{
    string? placesFilePath = GetPlacesFilePath(args);

    return placesFilePath is null
        ? GetDemoPlaces()
        : LoadPlaces(placesFilePath, jsonOptions);
}

static string? GetPlacesFilePath(string[] args)
{
    for (int index = 0; index < args.Length; index++)
    {
        string argument = args[index];

        if (argument.Equals("--eval", StringComparison.OrdinalIgnoreCase))
        {
            return GetOptionalOptionValue(args, index) ?? DefaultEvaluationFilePath;
        }

        if (argument.StartsWith("--eval=", StringComparison.OrdinalIgnoreCase))
        {
            return GetAssignedOptionValue(argument, "--eval=");
        }

        if (argument.Equals("--places", StringComparison.OrdinalIgnoreCase))
        {
            return GetRequiredOptionValue(args, index, "--places");
        }

        if (argument.StartsWith("--places=", StringComparison.OrdinalIgnoreCase))
        {
            return GetAssignedOptionValue(argument, "--places=");
        }
    }

    return null;
}

static string? GetOptionalOptionValue(string[] args, int optionIndex)
{
    int valueIndex = optionIndex + 1;

    return valueIndex < args.Length && !args[valueIndex].StartsWith("--", StringComparison.Ordinal)
        ? args[valueIndex]
        : null;
}

static string GetRequiredOptionValue(string[] args, int optionIndex, string optionName) =>
    GetOptionalOptionValue(args, optionIndex)
    ?? throw new ArgumentException($"Missing filename after {optionName}.");

static string GetAssignedOptionValue(string argument, string optionPrefix)
{
    string value = argument[optionPrefix.Length..];

    return string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException($"Missing filename after {optionPrefix.TrimEnd('=')}.")
        : value;
}

static Place[] LoadPlaces(string filePath, JsonSerializerOptions jsonOptions)
{
    string resolvedPath = Path.GetFullPath(filePath);
    string json = File.ReadAllText(resolvedPath);
    Place[]? places = JsonSerializer.Deserialize<Place[]>(json, jsonOptions);

    return places is { Length: > 0 }
        ? places
        : throw new JsonException("The places file must contain a non-empty JSON array of places.");
}

static Place[] GetDemoPlaces() =>
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
