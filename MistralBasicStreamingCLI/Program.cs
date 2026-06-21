// Copyright(c) June 2026, devMobile Software
//
// This program demonstrates how to call the Mistral API with streaming (Server-Sent Events)
//
// 
using MistralBasicStreamingCLI.Models;


IConfiguration configuration = new ConfigurationBuilder()
   .SetBasePath(AppContext.BaseDirectory)
   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
   .AddUserSecrets<Program>()
   .Build();

var settings = configuration.GetSection(nameof(ApplicationSettings)).Get<ApplicationSettings>() ?? throw new InvalidOperationException("Failed to load application settings");


Console.WriteLine("################");
Console.WriteLine("   ##      ##   ");
Console.WriteLine("   ##      ##   ");
Console.WriteLine("     ##  ##     ");
Console.WriteLine("   ##########   ");
Console.WriteLine("   ##  ##  ##   ");
Console.WriteLine(" ####  ##  #### ");
Console.WriteLine("################");


// Create HttpClient with required headers.
using HttpClient httpClient = new()
{
   DefaultRequestHeaders =
   {
      Accept = { new MediaTypeWithQualityHeaderValue("text/event-stream") },
      Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey)
   },
   BaseAddress = new Uri(settings.BaseUrl)
};

var jsonSerializerOptions = new JsonSerializerOptions()
{
   // PropertyNamingPolicy removed - [JsonPropertyName] attributes on model handle wire names
   WriteIndented           = false,
   DefaultIgnoreCondition  = JsonIgnoreCondition.WhenWritingNull,
   AllowTrailingCommas     = false,
   ReadCommentHandling     = JsonCommentHandling.Disallow,
   UnmappedMemberHandling  = JsonUnmappedMemberHandling.Skip,
};

Console.Write("Enter chat message: ");
var content = Console.ReadLine();

while (!string.IsNullOrWhiteSpace(content))
{
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

      using var httpResponse = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
      httpResponse.EnsureSuccessStatusCode();

      await using var stream = await httpResponse.Content.ReadAsStreamAsync();
      using var reader = new StreamReader(stream);

      TokenUsage? finalUsage = null;
      string? finishReason = null;

      string? line;
      while ((line = await reader.ReadLineAsync()) is not null)
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
   catch (HttpRequestException ex)
   {
      Console.WriteLine($"Request failed: {(int?)ex.StatusCode} {ex.Message}");
   }
   catch (TaskCanceledException)
   {
      Console.WriteLine("Request timed out.");
   }
   catch (JsonException ex)
   {
      Console.WriteLine($"Failed to parse response: {ex.Message}");
   }

   Console.Write("Enter chat message: ");
   content = Console.ReadLine();
}
