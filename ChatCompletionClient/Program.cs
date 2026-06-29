// Copyright(c) June 2026, devMobile Software

using System.Text;
using System.Text.Json.Nodes;

using ChatCompletionClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MistralAI.Chat;

// Host.CreateApplicationBuilder already loads appsettings.json (from the content
// root), environment variables, and command-line args. Pin ContentRootPath to the
// app directory so appsettings.json is found regardless of the caller's cwd.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// User secrets unconditionally (host only adds them in Development).
builder.Configuration.AddUserSecrets<Program>();

var settings = builder.Configuration
    .GetSection(nameof(ApplicationSettings))
    .Get<ApplicationSettings>()
    ?? throw new InvalidOperationException("Failed to load ApplicationSettings.");

if (string.IsNullOrWhiteSpace(settings.ApiKey))
    throw new InvalidOperationException("ApplicationSettings:ApiKey is missing.");
if (string.IsNullOrWhiteSpace(settings.BaseUrl))
    throw new InvalidOperationException("ApplicationSettings:BaseUrl is missing.");
if (string.IsNullOrWhiteSpace(settings.ModelName))
    throw new InvalidOperationException("ApplicationSettings:ModelName is missing.");

builder.Services.AddMistralChatClient(settings.BaseUrl, settings.ApiKey);

using var host = builder.Build();

// Start the host so IHostApplicationLifetime wires Ctrl+C / SIGINT to ApplicationStopping
// (and so the cancellation token actually fires on shutdown).
await host.StartAsync();

var client = host.Services.GetRequiredService<IMistralChatClient>();
var ct = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

var messages = new List<ChatMessage>();

Console.WriteLine($"Mistral Chat — model: {settings.ModelName}");
Console.WriteLine("Commands: streaming | buffered | <blank> to exit");
Console.WriteLine(new string('─', 50));

while (!ct.IsCancellationRequested)
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
        continue;  // Blank message cancels the turn but keeps the prompt loop running.

    messages.Add(new UserMessage { Content = JsonValue.Create(userInput)! });

    try
    {
        if (command == "streaming")
        {
            var request = new ChatCompletionRequest
            {
               Model = settings.ModelName,
               Messages = [.. messages]
            };

            var buffer = new StringBuilder();
            var prefixWritten = false;

            await foreach (var chunk in client.StreamAsync(request, ct))
            {
                if (chunk.Choices.Count == 0) continue;

                var text = ExtractText(chunk.Choices[0].Delta.Content);
                if (text.Length == 0) continue;

                if (!prefixWritten)
                {
                    Console.Write("Assistant: ");
                    prefixWritten = true;
                }

                Console.Write(text);
                buffer.Append(text);
            }

            if (prefixWritten) Console.WriteLine();
            messages.Add(new AssistantMessage { Content = JsonValue.Create(buffer.ToString()) });
        }
        else  // buffered
        {
            var request = new ChatCompletionRequest
            {
               Model = settings.ModelName,
               Messages = [.. messages]
            };

            var response = await client.CompleteAsync(request, ct);
            var content = response.Choices[0].Message.Content;
            var text = ExtractText(content);

            Console.WriteLine($"Assistant: {text}");
            messages.Add(new AssistantMessage { Content = content });
        }
    }
    catch (MistralApiException ex)
    {
        Console.WriteLine($"\n  [API error {(int)ex.StatusCode}: {ex.ErrorMessage ?? ex.ResponseBody}]");
        RollbackLastUserTurn(messages);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("\n  [Cancelled]");
        RollbackLastUserTurn(messages);
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n  [Error: {ex.Message}]");
        RollbackLastUserTurn(messages);
    }
}

await host.StopAsync();

Console.WriteLine("\nGoodbye.");

// Extracts plain text from a content node that may be a JSON string or a
// ContentChunk[] (e.g. [{"type":"text","text":"..."}, {"type":"image_url",...}]).
// Non-text chunks are skipped rather than silently dropped via a failed string cast.
static string ExtractText(JsonNode? content)
{
    if (content is null) return string.Empty;

    if (content is JsonValue jv && jv.TryGetValue(out string? s))
        return s ?? string.Empty;

    if (content is JsonArray array)
    {
        var sb = new StringBuilder();
        foreach (var node in array)
        {
            if (node is not JsonObject obj) continue;
            if (obj["type"]?.GetValue<string>() != "text") continue;
            if (obj["text"] is JsonValue tv && tv.TryGetValue(out string? text) && text is not null)
                sb.Append(text);
        }
        return sb.ToString();
    }

    return string.Empty;
}

static void RollbackLastUserTurn(List<ChatMessage> messages)
{
    if (messages.Count > 0 && messages[^1] is UserMessage)
        messages.RemoveAt(messages.Count - 1);
}
