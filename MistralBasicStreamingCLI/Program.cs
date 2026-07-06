// Copyright(c) June 2026, devMobile Software
//
// This program is horrible, it demonstrates how to call the Mistral API with streaming (Server-Sent Events)
//
using MistralBasicStreamingCLI.Models;


IConfiguration configuration = new ConfigurationBuilder()
   .SetBasePath(AppContext.BaseDirectory)
   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
   .AddUserSecrets<Program>()
   .Build();

var settings = configuration.GetSection(nameof(ApplicationSettings)).Get<ApplicationSettings>() ?? throw new InvalidOperationException("Failed to load application settings");

ArgumentException.ThrowIfNullOrWhiteSpace(settings.ApiKey, nameof(settings.ApiKey));
ArgumentException.ThrowIfNullOrWhiteSpace(settings.BaseUrl, nameof(settings.BaseUrl));
ArgumentException.ThrowIfNullOrWhiteSpace(settings.ModelName, nameof(settings.ModelName));

// Create HttpClient with required headers.
using HttpClient httpClient = new()
{
   DefaultRequestHeaders =
   {
      Accept = { new MediaTypeWithQualityHeaderValue("text/event-stream") },
      Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey)
   },
   BaseAddress = new Uri(settings.BaseUrl),
   // SSE responses are long-lived; rely on the CancellationToken (Ctrl+C) instead of a wall-clock timeout.
   Timeout = Timeout.InfiniteTimeSpan,
};

var jsonSerializerOptions = new JsonSerializerOptions()
{
   // PropertyNamingPolicy removed - [JsonPropertyName] attributes on model handle wire names
   WriteIndented = false,
   DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
   AllowTrailingCommas = false,
   ReadCommentHandling = JsonCommentHandling.Disallow,
   UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
};

// Ctrl+C cancels the current request and returns to the prompt; second Ctrl+C exits the process.
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
   if (!cts.IsCancellationRequested)
   {
      e.Cancel = true;
      cts.Cancel();
   }
};

while (!cts.IsCancellationRequested)
{
   Console.Write("Enter chat message: ");
   var content = Console.ReadLine();
   if (string.IsNullOrWhiteSpace(content)) break;

   var request = new ChatStreamingCompletionRequest
   {
      Model = settings.ModelName,
      Messages =
      [
         new ChatMessage { Role = "user", Content = content }
      ],
      Stream = true,
   };

   try
   {
      using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
      {
         Content = JsonContent.Create(request, options: jsonSerializerOptions)
      };

      using var httpResponse = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token);
      if (!httpResponse.IsSuccessStatusCode)
      {
         var body = await httpResponse.Content.ReadAsStringAsync(cts.Token);
         throw new HttpRequestException($"Mistral API {(int)httpResponse.StatusCode}: {body}", null, httpResponse.StatusCode);
      }

      await using var stream = await httpResponse.Content.ReadAsStreamAsync(cts.Token);
      using var reader = new StreamReader(stream);

      TokenUsage? finalUsage = null;
      string? finishReason = null;

      string? line;
      // Mistral emits single-line `data:` chunks, so per-line dispatch is sufficient (no multi-line SSE concatenation required).
      while ((line = await reader.ReadLineAsync(cts.Token)) is not null)
      {
         // SSE framing: blank line separates events, lines without "data:" are ignored (e.g. ":" keep-alive comments, "event:" / "id:" / "retry:" fields).
         if (string.IsNullOrEmpty(line)) continue;
         if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

         var payload = line.AsSpan(5).TrimStart();
         if (payload.SequenceEqual("[DONE]")) break;

         var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(payload, jsonSerializerOptions);
         if (chunk is null) continue;

         foreach (var choice in chunk.Choices)
         {
            if (!string.IsNullOrEmpty(choice.Delta.Content))
            {
               Console.Write(choice.Delta.Content);
            }
            if (choice.FinishReason is not null)
            {
               finishReason = choice.FinishReason;
            }
         }

         if (chunk.Usage is not null)
         {
            finalUsage = chunk.Usage;
         }
      }

      Console.WriteLine();
      Console.WriteLine();
      if (finalUsage is not null)
      {
         Console.WriteLine($"Prompt tokens: {finalUsage.PromptTokens}");
         Console.WriteLine($"Completion tokens: {finalUsage.CompletionTokens}");
         Console.WriteLine($"Total tokens: {finalUsage.TotalTokens}");
      }
      if (finishReason is not null)
      {
         Console.WriteLine($"Finish reason: {finishReason}");
      }
      Console.WriteLine();
   }
   catch (OperationCanceledException) when (cts.IsCancellationRequested)
   {
      Console.WriteLine();
      Console.WriteLine("Request cancelled.");
      // Reset the CTS so the next prompt iteration is cancellable again.
      cts.TryReset();
   }
   catch (HttpRequestException ex)
   {
      Console.WriteLine($"Request failed: {(int?)ex.StatusCode ?? 0} {ex.Message}");
   }
   catch (TaskCanceledException)
   {
      Console.WriteLine("Request timed out.");
   }
   catch (JsonException ex)
   {
      Console.WriteLine($"Failed to parse response: {ex.Message}");
   }
   catch (IOException ex)
   {
      Console.WriteLine($"Stream error: {ex.Message}");
   }
   catch (Exception ex)
   {
      Console.WriteLine($"Unexpected error: {ex.GetType().Name}: {ex.Message}");
   }
}
