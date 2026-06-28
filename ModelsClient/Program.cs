// Copyright(c) June 2026, devMobile Software

using ModelsClient;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddUserSecrets<Program>()
    .Build();

var settings = configuration.GetSection(nameof(ApplicationSettings)).Get<ApplicationSettings>()
    ?? throw new InvalidOperationException("Failed to load application settings.");

builder.Services.AddMistralModelsClient(settings.BaseUrl, settings.ApiKey);

using var host = builder.Build();

var client = host.Services.GetRequiredService<IMistralModelsClient>();
var ct = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

Console.WriteLine("Mistral Models CLI");
Console.WriteLine("Commands: list | get | delete | update | archive | unarchive | <blank to exit>");
Console.WriteLine(new string('─', 55));

while (true)
{
    Console.Write("\nCommand: ");
    var command = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();

    if (string.IsNullOrWhiteSpace(command))
        break;

    try
    {
        switch (command)
        {
            case "list":
            {
                Console.Write("  Provider filter (blank to skip): ");
                var provider = Console.ReadLine()?.Trim();
                Console.Write("  Model filter (blank to skip): ");
                var model = Console.ReadLine()?.Trim();

                var result = await client.ListAsync(
                    string.IsNullOrWhiteSpace(provider) ? null : provider,
                    string.IsNullOrWhiteSpace(model) ? null : model,
                    ct);

                Console.WriteLine($"\n  {result.Data.Count} model(s):\n");
                foreach (var m in result.Data)
                    PrintModelSummary(m);
                break;
            }

            case "get":
            {
                var modelId = PromptRequired("  Model ID: ");
                if (modelId is null) continue;

                var result = await client.RetrieveAsync(modelId, ct);
                PrintModelDetail(result);
                break;
            }

            case "delete":
            {
                var modelId = PromptRequired("  Model ID: ");
                if (modelId is null) continue;

                Console.Write($"  Delete '{modelId}'? (yes to confirm): ");
                if ((Console.ReadLine() ?? "").Trim().ToLowerInvariant() != "yes")
                {
                    Console.WriteLine("  Cancelled.");
                    continue;
                }

                var result = await client.DeleteAsync(modelId, ct);
                Console.WriteLine($"  Deleted: {result.Id}  deleted={result.Deleted}");
                break;
            }

            case "update":
            {
                var modelId = PromptRequired("  Model ID: ");
                if (modelId is null) continue;

                Console.Write("  New name (blank to skip): ");
                var name = Console.ReadLine()?.Trim();
                Console.Write("  New description (blank to skip): ");
                var description = Console.ReadLine()?.Trim();

                var request = new UpdateModelRequest
                {
                    Name = string.IsNullOrWhiteSpace(name) ? null : name,
                    Description = string.IsNullOrWhiteSpace(description) ? null : description,
                };

                var result = await client.UpdateAsync(modelId, request, ct);
                Console.WriteLine($"  Updated: {result.Id}");
                PrintModelDetail(result);
                break;
            }

            case "archive":
            {
                var modelId = PromptRequired("  Model ID: ");
                if (modelId is null) continue;

                var result = await client.ArchiveAsync(modelId, ct);
                Console.WriteLine($"  {result.Id}  archived={result.Archived}");
                break;
            }

            case "unarchive":
            {
                var modelId = PromptRequired("  Model ID: ");
                if (modelId is null) continue;

                var result = await client.UnarchiveAsync(modelId, ct);
                Console.WriteLine($"  {result.Id}  archived={result.Archived}");
                break;
            }

            default:
                Console.WriteLine("  Unknown command. Use: list | get | delete | update | archive | unarchive");
                break;
        }
    }
    catch (MistralApiException ex)
    {
        Console.WriteLine($"  [API error {(int)ex.StatusCode}: {ex.ResponseBody}]");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("  [Cancelled]");
        break;
    }
}

Console.WriteLine("\nGoodbye.");

static string? PromptRequired(string prompt)
{
    Console.Write(prompt);
    var value = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(value))
    {
        Console.WriteLine("  ID is required — skipping.");
        return null;
    }
    return value;
}

static void PrintModelSummary(ModelsClient.BaseModelCard m)
{
    var caps = new List<string>();
    if (m.Capabilities?.CompletionChat == true) caps.Add("chat");
    if (m.Capabilities?.CompletionFim == true) caps.Add("fim");
    if (m.Capabilities?.FunctionCalling == true) caps.Add("functions");
    if (m.Capabilities?.Vision == true) caps.Add("vision");
    if (m.Capabilities?.FineTuning == true) caps.Add("fine-tune");
    if (m.Capabilities?.Classification == true) caps.Add("classify");

    var archived = m.Archived ? " [archived]" : "";
    var ctx = m.MaxContextLength.HasValue ? $"{m.MaxContextLength.Value,7}" : "    n/a";
    Console.WriteLine($"  {m.Id,-45} owner:{m.OwnedBy ?? "?",-12} ctx:{ctx}  {string.Join(" ", caps)}{archived}");
}

static void PrintModelDetail(ModelsClient.BaseModelCard m)
{
    Console.WriteLine($"\n  Id           : {m.Id}");
    Console.WriteLine($"  Object       : {m.Object}");
    Console.WriteLine($"  Owned by     : {m.OwnedBy}");
    Console.WriteLine($"  Created      : {(m.Created > 0 ? DateTimeOffset.FromUnixTimeSeconds(m.Created).ToString("yyyy-MM-dd HH:mm:ss") + " UTC" : "n/a")}");
    Console.WriteLine($"  Root         : {m.Root ?? "n/a"}");
    Console.WriteLine($"  Archived     : {m.Archived}");
    Console.WriteLine($"  Max context  : {(m.MaxContextLength.HasValue ? m.MaxContextLength.Value.ToString("N0") : "n/a")}");
    Console.WriteLine($"  Aliases      : {(m.Aliases is { Count: > 0 } a ? string.Join(", ", a) : "none")}");

    if (m.Capabilities is { } c)
    {
        Console.WriteLine($"  Capabilities :");
        Console.WriteLine($"    Chat       : {c.CompletionChat}");
        Console.WriteLine($"    FIM        : {c.CompletionFim}");
        Console.WriteLine($"    Functions  : {c.FunctionCalling}");
        Console.WriteLine($"    Fine-tune  : {c.FineTuning}");
        Console.WriteLine($"    Vision     : {c.Vision}");
        Console.WriteLine($"    Classify   : {c.Classification}");
        Console.WriteLine($"    Reasoning  : {c.Reasoning}");
        Console.WriteLine($"    OCR        : {c.Ocr}");
        Console.WriteLine($"    Moderation : {c.Moderation}");
    }

    if (m is ModelsClient.FinetunedModelCard ft)
    {
        Console.WriteLine($"  Type         : {ft.Type}");
        Console.WriteLine($"  Job          : {ft.Job}");
    }
}
