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
    .AddEnvironmentVariables();

builder.Services
    .AddOptions<OpenRouterOptions>()
    .BindConfiguration(OpenRouterOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Explicitly add appsettings.json and environment variables
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables();

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

using IHost host = builder.Build();

var generator = host.Services.GetRequiredService<IDescriptionGenerator>();

Place place = new(
    Name: "Cape Disappointment State Park",
    Category: "Nature",
    Location: "Ilwaco, Washington",
    UserNotes: """
        Beautiful coastal park with dramatic cliffs.
        Historic lighthouse.
        Great place to watch storms and sunsets.
        """);

string markdown = await generator.GenerateAsync(place);

Console.WriteLine(markdown);