// Copyright(c) June 2026, devMobile Software

using System.Text;
using System.Text.Json.Nodes;

using ChatCompletionClient;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

IConfiguration configuration = new ConfigurationBuilder()
   .SetBasePath(AppContext.BaseDirectory)
   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
   .AddUserSecrets<Program>()
   .Build();

var settings = configuration.GetSection(nameof(ApplicationSettings)).Get<ApplicationSettings>() ?? throw new Exception("Failed to load application settings");

builder.Services.AddMistralChatClient(settings.BaseUrl, settings.ApiKey);

using var host = builder.Build();

var client = host.Services.GetRequiredService<IMistralChatClient>();
var ct = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

var messages = new List<ChatMessage>();

Console.WriteLine($"Mistral Chat  model: {settings.ModelName}");
Console.WriteLine("Commands: streaming | buffered | <blank> to exit");
Console.WriteLine(new string('─', 50));

while (true)
{
    Console.Write("\nCommand: ");
    var command = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();

    if (string.IsNullOrWhiteSpace(command))
        break;

    if (command is not ("streaming" or "buffered"))
    {
        Console.WriteLine("  Unknown command. Use 'streaming' or 'buffered'.");
        continue;
    }

    Console.Write("You: ");
    var userInput = (Console.ReadLine() ?? "").Trim();

    if (string.IsNullOrWhiteSpace(userInput))
        break;

    messages.Add(new UserMessage { Content = JsonValue.Create(userInput)! });

    var request = new ChatCompletionRequest
    {
        Model = settings.ModelName,
        Messages = [.. messages]
    };

    Console.Write("Assistant: ");

    try
    {
        if (command == "streaming")
        {
            var buffer = new StringBuilder();

            await foreach (var chunk in client.StreamAsync(request, ct))
            {
                if (chunk.Choices.Length == 0) continue;

                var delta = chunk.Choices[0].Delta.Content;
                var text = delta is JsonValue jv && jv.TryGetValue(out string? s) ? s ?? "" : "";

                Console.Write(text);
                buffer.Append(text);
            }

            Console.WriteLine();
            messages.Add(new AssistantMessage { Content = JsonValue.Create(buffer.ToString()) });
        }
        else  // buffered
        {
            var response = await client.CompleteAsync(request, ct);
            var content = response.Choices[0].Message.Content;
            var text = content is JsonValue jv && jv.TryGetValue(out string? s) ? s ?? "" : "";

            Console.WriteLine(text);
            messages.Add(new AssistantMessage { Content = content });
        }
    }
    catch (MistralApiException ex)
    {
        Console.WriteLine($"\n  [API error {(int)ex.StatusCode}: {ex.ResponseBody}]");
        messages.RemoveAt(messages.Count - 1);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("\n  [Cancelled]");
        break;
    }
}

Console.WriteLine("\nGoodbye.");
